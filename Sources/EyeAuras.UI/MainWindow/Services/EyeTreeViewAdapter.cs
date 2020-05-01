using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using EyeAuras.UI.Core.ViewModels;
using EyeAuras.UI.MainWindow.ViewModels;
using PoeShared.Scaffolding;
using PoeShared.UI.TreeView;
using ReactiveUI;

namespace EyeAuras.UI.MainWindow.Services
{
    internal sealed class EyeTreeViewAdapter : DisposableReactiveObject, IDisposableReactiveObject
    {
        private readonly SourceList<ITreeViewItemViewModel> treeViewSource;

        private ITreeViewItemViewModel selectedItem;
        private IAuraTabViewModel selectedValue;

        public EyeTreeViewAdapter()
        {
            treeViewSource = new SourceList<ITreeViewItemViewModel>();

            treeViewSource
                .Connect()
                .Bind(out var items)
                .Subscribe()
                .AddTo(Anchors);
            TreeViewItems = items;
        }

        public ReadOnlyObservableCollection<ITreeViewItemViewModel> TreeViewItems { get; }

        public ITreeViewItemViewModel SelectedItem
        {
            get => selectedItem;
            set => RaiseAndSetIfChanged(ref selectedItem, value);
        }

        public IAuraTabViewModel SelectedValue
        {
            get => selectedValue;
            set => RaiseAndSetIfChanged(ref selectedValue, value);
        }

        public void AddDirectory()
        {
            treeViewSource.Add(new DirectoryTreeViewItemViewModel(null) { Name = "Folder" });
        }

        public void SyncWith(ObservableCollection<IAuraTabViewModel> source)
        {
            treeViewSource.Clear();
            
            var root = new DirectoryTreeViewItemViewModel(null);

            source.ToObservableChangeSet()
                .ForEachItemChange(x =>
                {
                    switch (x.Reason)
                    {
                        case ListChangeReason.Add:
                        {
                            var item = new HolderTreeViewItemViewModel(x.Current, root);
                            treeViewSource.Add(item);
                        }
                            break;
                        case ListChangeReason.Moved:
                        {
                            treeViewSource.Move(x.PreviousIndex, x.CurrentIndex);
                        }
                            break;
                        case ListChangeReason.Remove:
                        {
                            var item = FindItemByTab(x.Current);
                            treeViewSource.Remove(item);
                        }
                            break;
                        case ListChangeReason.Clear:
                        {
                            treeViewSource.Clear();
                        }
                             break;
                        default:
                            throw new ArgumentOutOfRangeException($"Unsupported change: {x.Reason}");
                    }
                })
                .Subscribe()
                .AddTo(Anchors);

            source
                .ToObservableChangeSet()
                .WhenPropertyChanged(x => x.IsSelected)
                .Where(x => x.Value)
                .Subscribe(x => SelectedValue = x.Sender)
                .AddTo(Anchors);
        }

        private ITreeViewItemViewModel FindItemByTab(IAuraTabViewModel tab)
        {
            return Find(x => x is HolderTreeViewItemViewModel holderNode && holderNode.Value == tab).Single();
        }

        private IEnumerable<ITreeViewItemViewModel> Find(Predicate<ITreeViewItemViewModel> predicate,
            IEnumerable<ITreeViewItemViewModel> items = null)
        {
            foreach (var node in items ?? treeViewSource.Items)
            {
                if (node is IDirectoryTreeViewItemViewModel directoryNode)
                {
                    var matchingChildNodes = Find(predicate, directoryNode.Children);
                    foreach (var treeViewItemViewModel in matchingChildNodes)
                    {
                        yield return treeViewItemViewModel;
                    }
                }

                if (predicate(node))
                {
                    yield return node;
                }
            }
        }

        internal class RootTreeViewItemModel : DisposableReactiveObject, IDirectoryTreeViewItemViewModel
        {
            public bool IsExpanded { get; set; } = true;
            public bool IsSelected { get; set; } = false;
            public ITreeViewItemViewModel Parent { get; } = null;
            public string Name { get; set; } = "ROOT";
            public ObservableCollection<ITreeViewItemViewModel> Children { get; }
        }
    }

    internal class HolderTreeViewItemViewModel : TreeViewItemViewModel
    {
        public HolderTreeViewItemViewModel(IAuraTabViewModel tabViewModel, ITreeViewItemViewModel parent) : base(parent)
        {
            Value = tabViewModel;

            tabViewModel.WhenAnyValue(x => x.IsSelected)
                .Subscribe(x => IsSelected = x)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsSelected)
                .Subscribe(x => tabViewModel.IsSelected = x)
                .AddTo(Anchors);
        }

        public IAuraTabViewModel Value { get; }

        public override string ToString()
        {
            return $"Holder for {Value}";
        }
    }

    internal class DirectoryTreeViewItemViewModel : TreeViewItemViewModel
    {
        private string name;

        public DirectoryTreeViewItemViewModel(ITreeViewItemViewModel parent) : base(parent)
        {
        }

        public string Name
        {
            get => name;
            set => RaiseAndSetIfChanged(ref name, value);
        }

        public override string ToString()
        {
            return $"Directory {Name}";
        }
    }
}