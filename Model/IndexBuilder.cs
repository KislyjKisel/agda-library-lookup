using AgdaLibraryLookup.Agda.Connection;
using AgdaLibraryLookup.Collections;
using AgdaLibraryLookup.Functional;
using AgdaLibraryLookup.Lexer;
using AgdaLibraryLookup.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static AgdaLibraryLookup.Functional.FunctionalExt;
using static AgdaLibraryLookup.Maybe<string>;

namespace AgdaLibraryLookup.Model
{
    using NestingTC = NestingTraverseConstrainer<Token>;
    using PredicateTC = PredicateTraverseConstrainer<Token>;

    public delegate void DefinitionParsedHandler(Either<string, Definition> args);

    public sealed class IndexBuilder : INotifyPropertyChanged
    {
        public int DefinitionsProcessedCount { get; private set; }
        public ObservableCollection<string> ModulesProcessed => _modulesProcessed; 
        public ObservableCollection<string> Log => _log;

        private ObservableCollection<string> _modulesProcessed = new();
        private readonly ObservableCollection<string> _log = new();


        /// <param name="id"> Identifier of the definition to infer type of, relative to the file's top-level module. </param>
        private delegate Task<Either<string, string>> InferDefinitionType(string innerPath, string name);

        private void RunLog(Action<ObservableCollection<string>> act)
        {
            Application.Current.Dispatcher.Invoke(() => 
            { 
                act(_log); 
                NotifyPropertyChanged(nameof(Log)); 
            });
        }

        private void RunLog(string msg) => RunLog(log => log.Add(msg));

        /// <summary> Creates index files for specified library. </summary>
        /// <param name="libDir"> Directory containing library description file. </param>
        /// <returns> Left with error message or Right with library name. </returns>
        public async Task BuildIndexFor(string libDescrFile, ICommunicator agda) 
        {
            DefinitionsProcessedCount = 0;
            PropertyChanged?.Invoke(this, new(nameof(this.DefinitionsProcessedCount)));
            Application.Current.Dispatcher.Invoke(() => _modulesProcessed.Clear());
            PropertyChanged?.Invoke(this, new(nameof(this.ModulesProcessed)));

            string libName = File.ReadAllLines(libDescrFile).First(l => l.StartsWith("name:"))[5..].Trim();

            RunLog(log => { log.Clear(); log.Add($"Library: {libName}"); });

            string libIndexDir = Path.Combine("index", libName);

            IEnumerable<string> srcDirs = File.ReadAllLines(libDescrFile)
                                              .Select(l => l.TrimStart()
                                                            .Subst(lt => lt.StartsWith("include:") ? Just(lt) 
                                                                                                   : Nothing()))
                                              .Concat()
                                              .Select(l => l[8..].Split(new char[]{' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
                                              .Concat();

            int defCountMC = 0;
            var eeds = srcDirs.Select(srcDir => 
            {
                string fullSrcDir = Path.Combine(Path.GetDirectoryName(libDescrFile)!, srcDir);
                return ParseDefinitions(libName, fullSrcDir, fullSrcDir, agda, eErrDef => {
                    if(++defCountMC == _definitionsPerNotification)
                    {
                        DefinitionsProcessedCount += _definitionsPerNotification;
                        defCountMC = 0;
                        Application.Current.Dispatcher.Invoke(() => PropertyChanged?.Invoke(this, new(nameof(this.DefinitionsProcessedCount))));
                    }
                });
            }).Concat();

            BinaryWriter? indexFile = null;
            VariantBinarySerializer<Definition>? serializer = null;
            string? lastModule = null; 

            void onModuleProcessed()
            {
                Application.Current.Dispatcher.Invoke(() => _modulesProcessed.Add(lastModule!));
                Application.Current.Dispatcher.Invoke(() => PropertyChanged?.Invoke(this, new(nameof(this.ModulesProcessed))));
                indexFile.Write((byte)0);
                indexFile.Close();
            }

            await foreach(var eed in eeds)
            {
                eed.Bimap(RunLog,
                def =>
                {
                    if(lastModule is null || indexFile is null || serializer is null || def.M != lastModule)
                    {
                        if(indexFile is not null) onModuleProcessed();
                        lastModule = def.M;
                        string indexFilePath = Path.Combine(libIndexDir, lastModule + ".ali");
                        Directory.CreateDirectory(Path.GetDirectoryName(indexFilePath)!);
                        indexFile = new BinaryWriter(File.Open(indexFilePath, FileMode.Create));
                        serializer = Definition.GetSerializer(indexFile);
                    }
                    indexFile.Write((byte)1);
                    serializer.Serialize(def.D);
                });
            }
            if(indexFile is null) {
                RunLog("No definitions have been found in the library.");
                return;
            }
            onModuleProcessed();

            DefinitionsProcessedCount += defCountMC;
            PropertyChanged?.Invoke(this, new(nameof(this.DefinitionsProcessedCount)));
            RunLog("Finished.");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private const int _definitionsPerNotification = 5;

        private async IAsyncEnumerable<Either<string, (string M, Definition D)>> ParseDefinitions(string libName, string srcDirTopLevel, string srcDir, ICommunicator agda, DefinitionParsedHandler onDefParsed)
        {
            foreach (string subDir in Directory.EnumerateDirectories(srcDir))
            {
                await foreach (var def in this.ParseDefinitions(libName, srcDirTopLevel, subDir, agda, onDefParsed))
                {
                    yield return def;
                }
            }

            foreach (string file in Directory.EnumerateFiles(srcDir))
            {
                if(Path.GetExtension(file) != ".agda") continue;
                string moduleRelPath = Path.GetRelativePath(srcDirTopLevel, file)[..^5]; // w/o ext
                string moduleName = moduleRelPath.Replace('\\', '.').Replace('/', '.');
                var defs = ParseModule(File.ReadAllText(file), (ip, id) => InferType(agda, libName, moduleName, ip, id), onDefParsed);
                await foreach(var def in defs)
                {
                    yield return def.MapRight(d => (moduleRelPath, d));
                }
            }
        }

        private async Task<Either<string, string>> InferType(ICommunicator agda, string libName, string module, string innerPath, string name)
        {
            using var tmplib = new TempLibrary(new[]{ libName });



            var tmpMain = tmplib.CreateModule("Main", sw =>
            {
                if(innerPath.Length == 0) {
                    sw.WriteLine($"open import {module} public");
                }
                else
                {
                    sw.WriteLine($"import {module}");
                    sw.WriteLine($"open {module}.{innerPath[..^1]} public");
                }
                sw.WriteLine($"    using ({name})");
            },
            "--sized-types", "--guardedness");

            var tmpShield = tmplib.CreateModule("Shield", sw => 
            {
                sw.WriteLine($"import {tmpMain.FullName}");
            }, 
            "--sized-types", "--guardedness");

            var loadResult = await agda.SendRequestAsync(new LoadRequest(tmpMain.Path));

            return await loadResult.AsyncBindRight<string>(async _ =>
            {
                return await agda.SendRequestAsync(
                    new InferTypeRequest(
                        new()
                        {
                            FilePath = tmpShield.Path,
                            HighlightingMethod = HighlightingMethod.Direct
                        },
                        NormalizationMode.Normalised,
                        expr: $"{tmpMain.FullName}.{name}"
                ));
            });
        }

        private struct IndentationCtx
        {
            public int Value = 0;
            public bool IgnoreDefinitions = false;
            public string Path = String.Empty;
            public FunctionLikeKind Kind = FunctionLikeKind.Function;
        }

        private enum IndentationKey
        {
            None,
            Data,
            Record,
            Field,
            Generic,
            GenericIgnored
        }

        private static async IAsyncEnumerable<Either<string, Definition>> ParseModule(string moduleSource, InferDefinitionType inferType, DefinitionParsedHandler onDefParsed)
        {
            using var sr = new StringReader(moduleSource);

            Stack<IndentationCtx> indentationStack = new Stack<IndentationCtx>();
            indentationStack.Push(new());
            IndentationKey indentKey = IndentationKey.None;

            string? innerModuleName = null;

            var curTypeRaw = new StringBuilder();
            string? lastId = null;

            bool foundTopLevelModule = false; // to ignore when building inner module path

            bool charIsNotWhiteSpace(char c) => !Char.IsWhiteSpace(c);

            IndentationCtx ctx() => indentationStack.Peek();

            Task<Either<string, Definition>> genfdef(string id, string typeRaw)
                => genfdefK(id, typeRaw, ctx().Kind);

            async Task<Either<string, Definition>> genfdefK(string id, string typeRaw, FunctionLikeKind kind)
            {
                return (await inferType(ctx().Path, id)).MapRight(
                   nt => (Definition)new FunctionDefinition(
                       name: id,
                       innerPath: ctx().Path,
                       type: typeRaw,
                       typeNormalized: Agda.Type.Parse(UnifyIntroducedVariableNames(nt)),
                       kind: kind
                   ));
            }

            async Task<Either<string, Definition>> onFdefParsed()
            {
                var res = await genfdef(lastId, ": " + curTypeRaw.ToString());
                onDefParsed(res);

                lastId = null;
                curTypeRaw.Clear();
                return res;
            }

            string? line;
            while ((line = sr.ReadLine()) is not null)
            {
            process_line:
                // todo: use traverse constrainers over line
                int i = 0;

                void moveWhile(Predicate<char> predicate)
                {
                    while (i < line!.Length && predicate(line[i])) ++i;
                }

                string takeWhile(Predicate<char> predicate)
                {
                    int i0 = i;
                    moveWhile(predicate);
                    return line![i0..i];
                }

                if (line.All(Char.IsWhiteSpace)) continue;

                moveWhile(Char.IsWhiteSpace);
                int indentation = i;

                if (indentKey != IndentationKey.None && ctx().Value <= indentation)
                {
                    indentationStack.Push(new()
                    {
                        Value = indentation,
                        IgnoreDefinitions = (indentKey == IndentationKey.GenericIgnored) || ctx().IgnoreDefinitions,
                        Path = ctx().Path + (innerModuleName is null ? String.Empty : innerModuleName + "."),
                        Kind = indentKey switch 
                        { 
                            IndentationKey.Data => FunctionLikeKind.DataConstructor, 
                            IndentationKey.Field => FunctionLikeKind.RecordField, 
                            _ => ctx().Kind 
                        }
                    });
                }
                else
                {
                    if (lastId is not null && ctx().Value > indentation)
                    {
                        yield return await onFdefParsed();
                    }
                    while (ctx().Value > indentation)
                    {
                        indentationStack.Pop();
                    }
                }

                indentKey = IndentationKey.None;

                string id = takeWhile(charIsNotWhiteSpace);
                if (lastId is null)
                {
                    if (id.Length == 0)
                    {
                        continue;
                    }
                    if (id == "module")
                    {
                        indentKey = IndentationKey.Generic;

                        if (!foundTopLevelModule)
                        {
                            foundTopLevelModule = true;
                            continue;
                        }

                        moveWhile(Char.IsWhiteSpace);
                        innerModuleName = takeWhile(charIsNotWhiteSpace);

                        if (innerModuleName.Length > 0)
                        {
                            if (innerModuleName == "_") innerModuleName = null;
                        }
                        else innerModuleName = null;

                        continue;
                    }

                    if(id == "record")
                    {
                        indentKey = IndentationKey.Record;

                        moveWhile(Char.IsWhiteSpace);
                        innerModuleName = takeWhile(charIsNotWhiteSpace);

                        if (innerModuleName.Length == 0)
                            innerModuleName = null;
                        else
                        {
                            // Record as definition
                            int i1 = line.Length - 1;
                            while (Char.IsWhiteSpace(line[i1])) --i1;
                            i1 = Math.Max(i, i1 - 5);

                            var res = await genfdefK(innerModuleName, line[i..i1], FunctionLikeKind.RecordType);
                            onDefParsed(res);
                            yield return res;
                        }

                        continue;
                    }

                    innerModuleName = null; // id is not module, reset last module name

                    if (id == "field")
                    {
                        indentKey = IndentationKey.Field;
                        if(line[5..].Any(charIsNotWhiteSpace))
                        {
                            moveWhile(Char.IsWhiteSpace);
                            indentationStack.Push(new() {
                                Path = ctx().Path,
                                IgnoreDefinitions = ctx().IgnoreDefinitions,
                                Kind = FunctionLikeKind.RecordField,
                                Value = i
                            });
                            indentKey = IndentationKey.None;

                            string fid = takeWhile(charIsNotWhiteSpace);
                            moveWhile(c => Char.IsWhiteSpace(c) || c == ':');
                            var res = await genfdef(fid, line[i..]);
                            onDefParsed(res);
                            yield return res;
                        }
                        continue;
                    }

                    if (id == "constructor")
                    {
                        moveWhile(Char.IsWhiteSpace);
                        var res = Either<string, Definition>.Right(new RecordConstructorDefinition(takeWhile(charIsNotWhiteSpace), ctx().Path));
                        onDefParsed(res);
                        yield return res;
                        continue;
                    }

                    if (id == "data")
                    {
                        indentKey = IndentationKey.Data;

                        moveWhile(Char.IsWhiteSpace);
                        string dataId = takeWhile(charIsNotWhiteSpace);

                        int i1 = line.Length - 1;
                        while(Char.IsWhiteSpace(line[i1])) --i1;
                        i1 = Math.Max(i, i1 - 5);

                        var res = await genfdefK(dataId, line[i..i1], FunctionLikeKind.DataType);
                        onDefParsed(res);
                        yield return res;
                        continue;
                    }

                    if (id == "pattern")
                    {
                        moveWhile(Char.IsWhiteSpace);
                        string patternId = takeWhile(charIsNotWhiteSpace);
                        var res = await genfdefK(patternId, line[i..], FunctionLikeKind.Pattern);
                        onDefParsed(res);
                        yield return res;
                        continue;
                    }


                    if (id == "postulate" || id == "interleaved" || id == "instance")
                    {
                        indentKey = IndentationKey.Generic;
                        continue;
                    }
                    if (id == "private" || id == "where" || id == "macro" || id == "variable")
                    {
                        indentKey = IndentationKey.GenericIgnored;
                        continue;
                    }

                    moveWhile(Char.IsWhiteSpace);
                    if (i >= line.Length || line[i++] != ':') continue;
                    moveWhile(Char.IsWhiteSpace);
                    if (ctx().IgnoreDefinitions) continue;
                    curTypeRaw.Clear();
                    curTypeRaw.Append(line[i..]);
                    lastId = id;
                }
                else if (indentation <= ctx().Value)
                {
                    yield return await onFdefParsed();
                    goto process_line;
                }
                else if (indentation > ctx().Value)
                {
                    curTypeRaw.Append(' ').Append(line[indentation..^0]).Append(' ');
                }
            }

            if (lastId is not null)
                yield return await onFdefParsed();
        }

        public static List<Token> UnifyIntroducedVariableNames(string raw)
        {
            raw = raw.Replace("\\n", " ");
            var toks = grammar.Tokenize(raw).ToList();
            var toktrav = new ListTraverser<Token>(toks);

            void insertUnifiedName(int offset, string name)
                => toks[toktrav.IndexAt(offset)] = new("name", name, toktrav[offset].Region);

            Dictionary<string, int> map = new Dictionary<string, int>();
            int vi = 0;

            //        discard   id
            List<Either<Unit, string>> consumeIntroIds(int off, int offMax)
            {
                List<Either<Unit, string>> consumedIds = new();
                while (off < offMax)
                {
                    var t = toktrav[off];
                    if(t.Tag == "discard") 
                    {
                        ++off;
                        consumedIds.Add(Either<Unit, string>.Left(Unit.Value));
                        continue;
                    }
                    string id = t.Value; //raw[(Range)t.Region];
                    if (!map.ContainsKey(id)) 
                    {
                        consumedIds.Add(id);
                        map.Add(id, vi++);
                    }

                    insertUnifiedName(off++, GetUnifiedVariableName(map[id]));
                }
                return consumedIds;
            }

            static Predicate<Token> tag_is(string val) => t => t.Tag == val;
            static bool pi_intro_parenL(Token t)
                => t.Tag == "parenL" || t.Tag == "curlyL" || t.Tag == "instsL";
            static bool pi_intro_parenR(Token t)
                => t.Tag == "parenR" || t.Tag == "curlyR" || t.Tag == "instsR";

            while (!toktrav.EndOfList())
            {
                Token tCur = toktrav.Get();
                Maybe<Token> tNext = toktrav.GetM(1);

                if (tCur.Tag == "lambda")
                {
                    if (tNext.Map(tag_is("curlyL")).Default(false))
                    {
                        toktrav.Move(
                            toktrav.FindOffset(
                                new NestingTC(tag_is("curlyL"), tag_is("curlyR"), 0), 1
                            )
                            .Default(int.MaxValue - toktrav.IndexAt(0))); // if pattern matching lambda never ends - skip to end (maybe throw?)

                        // pattern matching lambda
                        // throw new NotImplementedException(); // todo: unsupported types (with pattern matching lambdas) mustn't throw
                        // Hard to parse
                        // - multiple cases (separated with ';' which can appear in inner exrpessions)
                        // - constructors can be operators and it may be hard to get their full name
                        // ex. (with infix pair constructor "_,_")
                        // \{ (a , b) -> a; (a , zero) -> zero }
                        // (1 - maybe inner ';'s can appear only inside {} (record syntax) ?)
                        // (2 - maybe agda2-interactive will recognize "," without "_"'s?)
                    }
                    else
                    {
                        // default lambda
                        int count = toktrav.Count(new PredicateTC(tag_is("name")), 1);
                        toktrav.Move(1);
                        if (toktrav.GetM(count).Map(tag_is("arrow")).Default(false))
                        {
                            consumeIntroIds(0, count);
                            toktrav.Move(count);
                        }
                    }
                }
                else if (pi_intro_parenL(tCur))
                {
                    Token openParen = tCur;
                    // pi-type
                    int piTypeStart = toktrav.IndexAt();
                    int count = toktrav.Count(new PredicateTC(t => t.Tag == "name" || t.Tag == "discard"), 1);
                    toktrav.Move(1);
                    if (toktrav.GetM(count).Map(tag_is("colon")).Default(false))
                    {
                        List<Either<Unit, string>> consumedIds = consumeIntroIds(0, count);
                        toktrav.Move(count + 1); // skip all consumed intros and colon

                        int piTypeExprStart = toktrav.IndexAt();
                        while(!toktrav.EndOfList() && !pi_intro_parenR(toktrav.Get(0)))
                        {
                            var tok = toktrav.Get(0);
                            if (tok.Tag == "name")
                            {
                                bool consumedNow = false;
                                for(int i = 0; i < consumedIds.Count; ++i)
                                {
                                    consumedIds[i].MapRight(id => { 
                                        if(tok.Value == id)
                                        {
                                            consumedNow = true;
                                        }
                                    });
                                    if(consumedNow) break;
                                }
                                if(!consumedNow && map.TryGetValue(tok.Value, out int idUi))
                                {
                                    insertUnifiedName(0, GetUnifiedVariableName(idUi));
                                }
                            }
                            toktrav.Move();
                        }
                        if(!toktrav.EndOfList()) 
                        { 
                            Token closingParen = toktrav.Get();
                            int piTypeExprEnd = toktrav.IndexAt();
                            toktrav.Move();
                            var piTypeExpr = toks.Range(piTypeExprStart..piTypeExprEnd).ToList(); // copy so inserting tokens wont affect expr range

                            int i = piTypeStart;
                            int iM = toktrav.IndexAt();
                            int toksInserted = 0;

                            void insertT(Token tok)
                            {
                                if(i < iM)
                                {
                                    toks[i] = tok;
                                }
                                else
                                {
                                    toks.Insert(i, tok);
                                    ++toksInserted;
                                }
                                ++i;
                            }

                            foreach (var ciid in consumedIds)
                            {
                                insertT(openParen);
                                insertT(new("name", ciid.Fold(_ => "_", id => GetUnifiedVariableName(map[id])), default));
                                insertT(new("colon", ":", default));
                                foreach(var etok in piTypeExpr)
                                {
                                    insertT(etok);
                                }
                                insertT(closingParen);
                            }

                            // int toksInserted = consumedIds.Count * (4 + piTypeExpr.Count); // { id : expr }
                            toktrav.Move(toksInserted);
                        }
                    }
                }
                else
                {
                    if (tCur.Tag == "name")
                    {
                        //non-intro id
                        if (map.TryGetValue(tCur.Value, out int idUi))
                        {
                            insertUnifiedName(0, GetUnifiedVariableName(idUi));
                        }
                    }
                    toktrav.Move();
                }
            }

            return toks;
        }

        private static readonly List<string> _unifiedVariableNames = new();
        private static string GetUnifiedVariableName(int index)
        {
            // @ used to avoid id collisions (@ can't be used in ids)
            // todo: check if @ used in other contexts can cause problems (irrel? tag will be different / they are not parsed?)

            for (int i = _unifiedVariableNames.Count; i <= index; ++i)
                _unifiedVariableNames.Add($"@{i}"); 

            return _unifiedVariableNames[index];
        }

        // Ignores '\'-lambdas, because in normalized form agda returns only 'λ's and can return '\n' (newline?) which can cause confusion.
        private static readonly Grammar grammar = Grammar.Parse(
                            @"
                                comment     < /(?:--.*(?:$|\n))|(?:{-.*-})/
                                lit_str     < /\"".*\""/
                                lit_chr     < /\'.\'/
                                lit_dec     < /-?\d[_\d]*/
                                lit_bin     < /-?0b[01][_01]*/
                                lit_hex     < /-?0x[\da-fA-F][_\da-fA-F]*/
                                lit_frc     < /-?\d+(?:\.\d+)?(?:[eE][+-]\d+)?/
                                colon       < /:/
                                arrow       < /(?:->)|(?:→)/
                                lambda      < /[λ]/
                                parenL      < /\(/
                                parenR      < /\)/
                                curlyL      < /\{/
                                curlyR      < /\}/
                                instsL      < /(?:{{)|⦃/
                                instsR      < /(?:}})|⦄/
                                name_part_x > /[^\s_\.\;\{\}\(\)\@\""\\]/
                                name_part   > /(name_part_x)(?:(name_part_x)|')*/
                                name_unqlf  > /_?(name_part)(?:_(name_part))*_?/
                                name        < /(name_unqlf)(?:\.(name_unqlf))*/
                                discard     < /_/
                            ",
                            (s, l) => { throw new FormatException($"{l}:{s}"); }
                        );

        private void NotifyPropertyChanged(string propName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
