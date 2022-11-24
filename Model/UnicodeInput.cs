using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using static AgdaLibraryLookup.Maybe<string>;

namespace AgdaLibraryLookup.Model
{
    public sealed class UnicodeInputTree
    {
        private const string DictionaryFilePath = "uims.json";
        private static UnicodeInputTree? _instance;

        public static UnicodeInputTree Instance
        {
            get
            {
                if(_instance is null) 
                {
                    if(!File.Exists(DictionaryFilePath)) 
                    {
                        File.CreateText(DictionaryFilePath).WriteLine(@"{ "" "": ""\"" }");
                    }
                    _instance = new UnicodeInputTree(File.ReadAllText(DictionaryFilePath));
                }
                return _instance;
            }
        }

        private UnicodeInputTree(string jsonDict)
        {
            void dictAppend(Dictionary<char, Node> dict, char key, Node n)
            {
                if (!dict.TryGetValue(key, out Node? old))
                {
                    dict.Add(key, n);
                    return;
                }
                if (old is not Branch oldb || n is not Branch nb)
                    throw new JsonException("Leaf key duplication.");

                foreach (var (k, v) in oldb.Nodes)
                {
                    dictAppend((Dictionary<char, Node>)nb.Nodes, k, v);
                }
                dict.Remove(key);
                dict.Add(key, nb);
            }

            Node desElement(JsonElement el)
            {
                if (el.ValueKind == JsonValueKind.Object)
                {
                    Dictionary<char, Node> nodes = new();
                    Maybe<string> defaultValue = Nothing();
                    foreach (var x in el.EnumerateObject())
                    {
                        if (x.Name.All(Char.IsWhiteSpace))
                        {
                            if (x.Value.ValueKind != JsonValueKind.String) throw new JsonException("Unexpected value kind.");
                            defaultValue = Just(x.Value.GetString()!);
                        }
                        else if (x.Name.StartsWith(' ') && x.Name.Length > 1 && !Char.IsWhiteSpace(x.Name[1]))
                        {
                            Dictionary<char, Node> singleDict = nodes;
                            int i = 1;
                            for (; i + 1 < x.Name.Length && !Char.IsWhiteSpace(x.Name[i + 1]); ++i)
                            {
                                Dictionary<char, Node> nextDict = new();
                                Branch node = new Branch(nextDict, Nothing());
                                //singleDict.Add(x.Name[i], node);
                                dictAppend(singleDict, x.Name[i], node);
                                singleDict = nextDict;
                            }
                            singleDict.Add(x.Name[i], desElement(x.Value));
                        }
                        else
                        {
                            if (x.Name.Length != 1 && Char.IsWhiteSpace(x.Name[0])) throw new JsonException("Expected single non-whitespace character or zero or more non-whitespace characters prefixed with single space.");
                            //nodes.Add(x.Name[0], desElement(x.Value));
                            dictAppend(nodes, x.Name[0], desElement(x.Value));
                        }
                    }
                    return new Branch(nodes, defaultValue);
                }
                if (el.ValueKind == JsonValueKind.String)
                {
                    return new Leaf(el.GetString()!);
                }
                throw new JsonException("Unexpected value kind.");
            }

            Node root = desElement(JsonDocument.Parse(jsonDict).RootElement);
            if (root is not Branch rootBranch || (bool)rootBranch.Value) throw new JsonException("Bad root element.");
            Root = rootBranch;
        }

        public Branch Root { get; init; }

        public abstract record Node;
        public sealed   record Branch(IReadOnlyDictionary<char, Node> Nodes, Maybe<string> Value) : Node;
        public sealed   record Leaf(string Value) : Node;
    }

    public sealed class UnicodeInput : INotifyPropertyChanged
    {
        public Maybe<(int Offset, string Value)> Process(char key)
            => _state.ToEither().Fold(
                nothing => { 
                    if(key == '\\')
                    {
                        _state = (1, UnicodeInputTree.Instance.Root);
                        NotifyIsActiveChanged();
                    }
                    return Nothing<(int, string)>();
                },
                st      => {
                    Maybe<(int, string)> res = Nothing<(int, string)>();
                    int off = st.Offset + 1;
                    if (!st.N.Nodes.TryGetValue(key, out UnicodeInputTree.Node? nextNode))
                    {
                        _state = Nothing<(int, UnicodeInputTree.Branch)>();
                        NotifyIsActiveChanged();
                    }
                    else if(nextNode is UnicodeInputTree.Branch nextBranch)
                    {
                        nextBranch.Value.Map(val => { res = (off, val); off = val.Length; });
                        _state = (off, nextBranch);
                    }
                    else if(nextNode is UnicodeInputTree.Leaf nextLeaf)
                    {
                        res = (off, nextLeaf.Value);
                        _state = Nothing<(int, UnicodeInputTree.Branch)>();
                        NotifyIsActiveChanged();
                    }
                    return res;
                }
            );

        public bool IsActive => (bool)_state;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyIsActiveChanged() 
            => PropertyChanged?.Invoke(this, new(nameof(IsActive)));


        private Maybe<(int Offset, UnicodeInputTree.Branch N)> _state;
    }
}
