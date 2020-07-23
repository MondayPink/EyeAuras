using System;
using System.Linq;
using System.Reactive.Linq;
using log4net;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI.TreeView;
using ReactiveUI;

namespace EyeAuras.UI.MainWindow.ViewModels
{
    internal class DirectoryTreeViewItemViewModel : EyeTreeItemViewModel, IDirectoryTreeViewItemViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DirectoryTreeViewItemViewModel));

        private string path;

        public DirectoryTreeViewItemViewModel(EyeTreeItemViewModel parent) : base(parent)
        {
            Observable.Merge(
                    this.WhenAnyValue(x => x.Parent)
                        .Select(x => x is DirectoryTreeViewItemViewModel eyeItem ? eyeItem.WhenAnyValue(y => y.Name) : Observable.Return(string.Empty))
                        .Switch()
                        .Select(_ => "Aura name changed"),
                    this.WhenAnyValue(x => x.Name).Select(_ => "Directory name changed"))
                .Select(x => FindPath(this))
                .WithPrevious((prev, curr) => new { prev, curr })
                .Where(x => x.prev != x.curr)
                .DistinctUntilChanged()
                .Subscribe(x =>
                {
                    Log.Debug($"[{this}] Changing Directory Path {x.prev} => {x.curr}");
                    Path = x.curr;
                })
                .AddTo(Anchors);
            
            EnableAurasCommand = CommandWrapper.Create(() => this.FindChildrenOfType<HolderTreeViewItemViewModel>().Select(x => x.Value).ForEach(x => x.IsEnabled = true));
            DisableAurasCommand = CommandWrapper.Create(() => this.FindChildrenOfType<HolderTreeViewItemViewModel>().Select(x => x.Value).ForEach(x => x.IsEnabled = false));
        }
        
        public CommandWrapper EnableAurasCommand { get; }
        
        public CommandWrapper DisableAurasCommand { get; }
        
        public string Path
        {
            get => path;
            private set => RaiseAndSetIfChanged(ref path, value);
        }
        
        public override string ToString()
        {
            return $"Directory {Name}";
        }
    }
}