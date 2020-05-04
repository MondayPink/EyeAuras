using System;
using System.Reactive.Linq;
using System.Windows.Input;
using EyeAuras.UI.Core.ViewModels;
using log4net;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;

namespace EyeAuras.UI.MainWindow.Services
{
    internal class HolderTreeViewItemViewModel : EyeTreeItemViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HolderTreeViewItemViewModel));

        public HolderTreeViewItemViewModel(IAuraTabViewModel tabViewModel, EyeTreeItemViewModel parent) : base(parent)
        {
            Value = tabViewModel;

            tabViewModel.WhenAnyValue(x => x.IsSelected)
                .Subscribe(x => IsSelected = x)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsSelected)
                .Subscribe(x => tabViewModel.IsSelected = x)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.Parent)
                .Select(x => x is DirectoryTreeViewItemViewModel eyeItem ? eyeItem.WhenAnyValue(y => y.Path) : Observable.Return(string.Empty))
                .Switch()
                .Select(x => FindPath(this))
                .WithPrevious((prev, curr) => new { prev, curr })
                .Subscribe(x =>
                {
                    Log.Debug($"[{this}] Changing Aura Path {x.prev} => {x.curr}");
                    Value.Path = x.curr;
                })
                .AddTo(Anchors);

            Value.WhenAnyValue(x => x.TabName)
                .Subscribe(x =>
                {
                    tabName.SetValue(x);
                    tabName.SetDefaultValue(x);
                })
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.Name)
                .Subscribe(x =>
                {
                    Value.TabName = x;
                })
                .AddTo(Anchors);
        }

        public IAuraTabViewModel Value { get; }
        
        public override string ToString()
        {
            return $"Holder for {Value}";
        }
    }
}