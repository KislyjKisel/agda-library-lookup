using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AgdaLibraryLookup.Lexer
{
    public sealed class Grammar
    {
        public static Grammar Parse(string source, Action<string, int> errorHandler)
        {
            var regexMacro = new Regex(@"\((\w+)\)");
            var sb = new StringBuilder();
            var g = new Grammar();
            var rulesRaw = new List<(int Line, string Name, string Expr)>();
            var macros = new List<(HashSet<string> Deps, string Name, string Value)>();
            {
                int i = 0;
                int line = 1;
                void skipWhiteSpace()
                {
                    while (i < source.Length && Char.IsWhiteSpace(source[i]))
                        line = source[i++] == '\n' ? line + 1 : line;
                }
                skipWhiteSpace();
                while (true)
                {
                    sb.Clear();
                    while (i < source.Length && (Char.IsLetterOrDigit(source[i]) || source[i] == '_'))
                        sb.Append(source[i++]);
                    string name = sb.ToString();
                    sb.Clear();
                    if (name.Length == 0) break;
                    if (i == source.Length)
                    {
                        errorHandler($"Expected operator after {name}.", line);
                        continue;
                    }
                    skipWhiteSpace();
                    char op = source[i++];
                    if (op != '>' && op != '<')
                    {
                        errorHandler($"Invalid operator.", line);
                        --i;
                        op = '>';
                    }
                    int exprLine = -1;
                    while (i < source.Length)
                    {
                        skipWhiteSpace();
                        if (i >= source.Length || source[i] != '/')
                        {
                            break;
                        }
                        ++i;
                        while (i < source.Length && source[i] != '/')
                        {
                            if (Char.IsWhiteSpace(source[i]))
                            {
                                errorHandler("Expected regex right boundary character '/'.", line++);
                                break;
                            }
                            sb.Append(source[i++]);
                        }
                        ++i;
                        exprLine = line;
                        skipWhiteSpace();
                    }
                    string regex = sb.ToString();
                    if (regex.Length == 0)
                    {
                        errorHandler("Expected regex left boundary character '/'.", exprLine);
                        continue;
                    }
                    if (op == '<')
                    {
                        rulesRaw.Add((exprLine, name, regex));
                    }
                    else
                    {
                        HashSet<string> deps = new HashSet<string>();
                        string expr = sb.ToString();
                        var usedMacro = regexMacro.Matches(expr);
                        foreach (Match m in usedMacro) deps.Add(m.Groups[1].Value);
                        macros.Add((deps, name, expr));
                    }
                }
            }

            Dictionary<int, List<int>> mapMacroDependenants = new Dictionary<int, List<int>>();
            for (int i = 0; i < macros.Count; ++i)
            {
                var m = macros[i];
                var mdis = m.Deps.Select(dn => macros.FindIndex(mx => mx.Name == dn));
                if (!mapMacroDependenants.ContainsKey(i))
                    mapMacroDependenants.Add(i, new List<int>());

                foreach (var di in mdis)
                {
                    if (!mapMacroDependenants.ContainsKey(di))
                        mapMacroDependenants.Add(di, new List<int>());
                    mapMacroDependenants[i].Add(di);
                }

            }
            DirectedGraph macroDepGraph = new DirectedGraph(mapMacroDependenants);
            g._macro.AddRange(macroDepGraph.TopologicalSort().Select(i => (macros[i].Name, macros[i].Value)));

            foreach (var rule in rulesRaw)
            {
                string regex = g.ExpandMacro(rule.Expr);
                if (!ValidateRule(regex))
                {
                    errorHandler($"Invalid characters in regex: {regex}.", rule.Line);
                    continue;
                }
                g._rules.Add(rule.Name, regex);
            }

            return g;
        }

        /// <summary> Ensures that no capturing groups can be in the regex. </summary>
        public static bool ValidateRule(string regex)
        {
            for(int i = 0; i < regex.Length; ++i)
            {
                // Don't allow capturing groups: ban '(', allow "\(", "(?:".
                // This ensures that no regex-valid capturing groups will pass.
                if(regex[i] == '(' && (i == 0 || regex[i - 1] != '\\') && (i + 2 >= regex.Length || regex[i + 1] != '?' || regex[i + 2] != ':')) 
                    return false;
            }
            return true;
        }

        public string ExpandMacro(string regex) 
            => _macro.Aggregate(regex, (r, m) => r.Replace($"({m.Key})", m.Value));

        public IEnumerable<Token> Tokenize(string text, Action<TextRegion> skippedHandler)
        {
            if (_rules.Count == 0) throw new InvalidOperationException("Grammar doesn't contains rules.");
            var tokens = new List<Token>();
            int i = 0, line = 1, i_lineStart = 0;
            foreach (Match m in FullRegex.Matches(text))
            {
                while (Char.IsWhiteSpace(text[i])) line = text[i++] == '\n' ? line + 1 : line;
                if (m.Index > i)
                { 
                    skippedHandler(new(new(line, i       - i_lineStart + 1, i), 
                                       new(line, m.Index - i_lineStart + 1, m.Index)));
                }

                int column0 = i - i_lineStart + 1;
                i = m.Index + m.Length;
                int column1 = i - i_lineStart + 1;
                for (int j = 1; j < m.Groups.Count; ++j)
                    if (m.Groups[j].Success)
                    {
                        tokens.Add(new(
                            Tag:    _groupNames![j - 1],
                            Value:  m.Groups[j].Value,
                            Region: new(new(line, column0, m.Index), 
                                        new(line, column1, m.Index + m.Length))
                        ));
                        break;
                    }
            }
            return tokens;
        }

        public IEnumerable<Token> Tokenize(string text) => Tokenize(text, (_) => { });



        private readonly Dictionary<string, string> _rules = new();
        private readonly List<(string Key, string Value)> _macro = new();
        private string[]? _groupNames = null;
        private Regex? _fullRegex = null;
        
        private Grammar() { }

        private Regex FullRegex
        { 
            get
            {
                if(_fullRegex is not null) return _fullRegex;
                var fullRegex = new StringBuilder();
                Array.Resize(ref _groupNames, _rules.Count);
                int i = 0;
                foreach(var (k, v) in _rules)
                {
                    if(i != 0) fullRegex.Append('|');
                    fullRegex.Append($"({v})");
                    _groupNames[i++] = k;
                }
                return _fullRegex = new Regex(fullRegex.ToString());
            }
        }
    }
}
