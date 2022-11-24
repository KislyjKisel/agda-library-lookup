using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgdaLibraryLookup
{
    public sealed class DirectedGraph
    {
        public DirectedGraph()                                 => _edges = new();
        public DirectedGraph(Dictionary<int, List<int>> edges) => _edges = edges;

        public DirectedGraph(IEnumerable<(int From, IEnumerable<int> To)> edges)
        {
            _edges = new Dictionary<int, List<int>>();
            foreach (var e in edges)
            {
                if (!_edges.ContainsKey(e.From))
                {
                    _edges.Add(e.From, new List<int>(e.To));
                }
                else
                {
                    _edges[e.From].AddRange(e.To);
                }
                foreach (var to in e.To)
                    if (!_edges.ContainsKey(to))
                        _edges.Add(to, new List<int>());
            }
        }

        public DirectedGraph(IEnumerable<(int From, int To)> edges)
        { 
            _edges = new Dictionary<int, List<int>>();
            foreach(var e in edges)
            {
                if(!_edges.ContainsKey(e.From))
                {
                    _edges.Add(e.From, new List<int>() { e.To });
                }
                else
                {
                    _edges[e.From].Add(e.To);
                }
                if (!_edges.ContainsKey(e.To)) _edges.Add(e.To, new List<int>());
            }
        }

        public IEnumerable<int> TopologicalSort()
        {
            var starts = new Stack<int>();
            var edges = new Dictionary<int, List<int>>(_edges);

            Dictionary<int, bool> used = new Dictionary<int, bool>(
                _edges.Select(p => new KeyValuePair<int, bool>(p.Key, false))
            );
;
            void pushStarts() { 
                foreach((int i, var _) in edges)
                {
                    if(!used[i] && edges.All(ts => !ts.Value.Contains(i))) 
                    {
                        starts.Push(i);
                        used[i] = true;
                    }
                }
            }

            pushStarts();
            while(starts.Count > 0)
            {
                int si = starts.Pop();
                yield return si;
                edges[si].Clear();
                pushStarts();
            }

            yield break;
        }

        public DirectedGraph Reverse()
        {
            Dictionary<int, List<int>> redges = new();
            foreach(var (i, dis) in _edges)
            {
                if(!redges.ContainsKey(i))
                    redges.Add(i, new List<int>());
                foreach(var di in dis) { 
                    if (!redges.ContainsKey(di))
                        redges.Add(di, new List<int>());
                    redges[di].Add(i);
                }
            }
            return new(redges);
        }



        private readonly Dictionary<int, List<int>> _edges;
    }
}
