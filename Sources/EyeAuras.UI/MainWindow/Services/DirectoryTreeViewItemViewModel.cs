using System.Reactive.Linq;
using System.Windows.Input;
using JetBrains.Annotations;
using log4net;
using PoeShared.Scaffolding;
using PoeShared.UI.TreeView;
using Prism.Commands;
using ReactiveUI;
using System;
using System.Linq;
using PoeShared.Scaffolding.WPF;

namespace EyeAuras.UI.MainWindow.Services
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
                        .ToUnit(),
                    this.WhenAnyValue(x => x.Name).ToUnit()
                    )
                .Select(x => FindPath(this))
                .WithPrevious((prev, curr) => new { prev, curr })
                .Subscribe(x =>
                {
                    Log.Debug($"[{this}] Changing Directory Path {x.prev} => {x.curr}");
                    Path = FindPath(this);
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