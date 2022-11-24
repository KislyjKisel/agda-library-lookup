using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace AgdaLibraryLookup.Model
{
    public class LibraryIndex : INotifyPropertyChanged
    {
        public LibraryIndex(string library, ModuleTreeBranch moduleTreeRoot)
        {
            Library = library;
            Modules = moduleTreeRoot;
            Modules.PropertyChanged += (_, pn) => {
                if(pn.PropertyName == nameof(Modules.Enabled)) 
                {
                    PropertyChanged?.Invoke(this, new(nameof(Enabled)));
                }
            };
        }

        public string           Library { get; init; }
        public ModuleTreeBranch Modules { get; init; }

        public bool? Enabled
        {
            get => Modules.Enabled;
            set => Modules.Enabled = value;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public override string ToString()
            => $"[{Library}] {Modules.ToString()}";
    }

    public abstract record ModuleTreeNode : INotifyPropertyChanged
    {
        public ModuleTreeBranch? Parent { get; init; } = null;

        public IEnumerable<(string Path, bool Enabled)> Traverse() => Traverse(new());
        
        public abstract bool? Enabled { get; set; }
        public abstract string Label { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void RaisePropertyChanged(string propname) => PropertyChanged?.Invoke(this, new(propname)); // shouldn't be exposed

        public abstract void SetEnabledPropagatingDown(bool? enabled);

        internal abstract IEnumerable<(string Path, bool Enabled)> Traverse(StringBuilder path); // shouldn't be exposed
    }

    public sealed record ModuleTreeBranch(string Tag, List<ModuleTreeNode> Nodes) : ModuleTreeNode
    {
        private bool? _enabled = false;

        public override bool? Enabled 
        {
            get => _enabled;
            set 
            { 
                SetEnabledPropagatingDown(value);
                RaisePropertyChanged(nameof(Enabled));
                Parent?.PropagateEnabledPropertyChangedUp(); 
            }
        }

        private void EvalEnabled()
        {
            _enabled = Nodes.All(n => n.Enabled.HasValue && n.Enabled.Value) ? true 
                     : (Nodes.Any(n => !n.Enabled.HasValue || n.Enabled.Value) ? null : false);/*Nodes.AllOrNone(p => p.Enabled.GetValueOrDefault(false));*/
        }

        public override void SetEnabledPropagatingDown(bool? value)
        {
            _enabled = value;
            Nodes.ForEach(n => { 
                n.SetEnabledPropagatingDown(value); 
                n.RaisePropertyChanged(nameof(n.Enabled)); 
            });
        }

        public void PropagateEnabledPropertyChangedUp()
        {
            EvalEnabled();
            RaisePropertyChanged(nameof(this.Enabled));
            Parent?.PropagateEnabledPropertyChangedUp();
        }

        public override string Label => Tag;

        internal override IEnumerable<(string Path, bool Enabled)> Traverse(StringBuilder path)
        {
            int sbl = path.Length;
            if(Tag.Length > 0) path.Append(Tag).Append('.'); // don't append '.' after empty tag (top-level module branch)
            foreach (var node in Nodes)
            {
                foreach (var value in node.Traverse(path))
                    yield return value;
            }
            path.Remove(sbl, path.Length - sbl);
        }

        public override string ToString()
            => $"{Label}({(Nodes.Count > 0 ? Nodes.Skip(1).Aggregate(new StringBuilder(Nodes[0].ToString()), (sb, s) => sb.Append(", ").Append(s)) : string.Empty)})";
    }

    public sealed record ModuleTreeLeaf(string Value) : ModuleTreeNode
    {
        public override bool? Enabled 
        { 
            get => _enabled;
            set 
            {
                _enabled = value.GetValueOrDefault(false);
                RaisePropertyChanged(nameof(Enabled));
                Parent?.PropagateEnabledPropertyChangedUp();
            }
        }

        public override void SetEnabledPropagatingDown(bool? enabled)
        {
            _enabled = enabled.GetValueOrDefault(false);
        }

        public override string Label => Value;

        internal override IEnumerable<(string Path, bool Enabled)> Traverse(StringBuilder path)
        {
            yield return (path.ToString() + Value, Enabled.GetValueOrDefault(false));
        }

        public override string ToString()
            => Label;


        private bool _enabled = false;
    }
}
