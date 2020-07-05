using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.Core.ViewModels;
using EyeAuras.UI.MainWindow.ViewModels;
using log4net;
using PoeShared;
using PoeShared.Scaffolding;
using PoeShared.UI.TreeView;
using ReactiveUI;

namespace EyeAuras.UI.MainWindow.Services
{
    internal sealed class EyeTreeViewAdapter : DisposableReactiveObject, IDisposableReactiveObject
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(EyeTreeViewAdapter));

        private ITreeViewItemViewModel selectedItem;
        private IAuraTabViewModel selectedValue;
        private readonly DirectoryTreeViewItemViewModel root = new DirectoryTreeViewItemViewModel(null) {};
        private ObservableCollection<IAuraTabViewModel> itemsSource;

        public EyeTreeViewAdapter()
        {
            TreeViewItems = root.Children;
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

        public ObservableCollection<IAuraTabViewModel> ItemsSource
        {
            get => itemsSource;
            private set => RaiseAndSetIfChanged(ref itemsSource, value);
        }

        public IEnumerable<AuraDirectoryProperties> EnumerateDirectories()
        {
            return root.FindChildren(x => x is DirectoryTreeViewItemViewModel).Cast<DirectoryTreeViewItemViewModel>().Select(x => new AuraDirectoryProperties
            {
                IsExpanded = x.IsExpanded,
                Path = x.Path
            });
        }

        public IEnumerable<IAuraTabViewModel> EnumerateAuras(string path)
        {
            var directory = FindDirectoryByPath(path);
            if (directory == null)
            {
                yield break;
            }

            foreach (var tabViewModel in directory.FindChildren(x => x is HolderTreeViewItemViewModel).Cast<HolderTreeViewItemViewModel>()
                .Select(x => x.Value))
            {
                yield return tabViewModel;
            }
        }

        public DirectoryTreeViewItemViewModel AddDirectory(string path)
        {
            const string defaultNewDirectoryName = "New folder";
            var idx = 1;
            while (true)
            {
                var fullPath = Path.Combine(path ?? string.Empty, idx == 1 ? defaultNewDirectoryName : $"{defaultNewDirectoryName} #{idx}");
                var existingDirectory = FindDirectoryByPath(fullPath);
                if (existingDirectory != null)
                {
                    idx++;
                }
                else
                {
                    return FindOrCreateByPath(fullPath);
                }
            }
        }

        public void RemoveDirectory(string path)
        {
            var existingDirectory = FindDirectoryByPath(path);
            if (existingDirectory == null)
            {
                throw new InvalidOperationException($"Could not find directory {path}");
            }
            var auras = existingDirectory.FindChildren(x => x is HolderTreeViewItemViewModel)
                .OfType<HolderTreeViewItemViewModel>()
                .Select(x => x.Value)
                .ToArray();
            ItemsSource.RemoveMany(auras);
            existingDirectory.Parent = null;
        }

        public void SyncWith(ObservableCollection<IAuraTabViewModel> source, AuraDirectoryProperties[] directoryProperties)
        {
            ItemsSource = source;
            root.Clear();

            foreach (var config in directoryProperties)
            {
                var directory = FindOrCreateByPath(config.Path);
                directory.IsExpanded = config.IsExpanded;
            }
            source.ToObservableChangeSet()
                .ForEachItemChange(x =>
                {
                    switch (x.Reason)
                    {
                        case ListChangeReason.Add:
                        {
                            var parent = FindOrCreateByPath(x.Current.Path);
                            var item = new HolderTreeViewItemViewModel(x.Current, parent);
                        }
                            break;
                        case ListChangeReason.Remove:
                        {
                            var item = FindItemByTab(x.Current);
                            item.Parent = null;
                        }
                            break;
                        case ListChangeReason.Clear:
                        {
                            root.Clear();
                        }
                             break;
                        default:
                            throw new ArgumentOutOfRangeException($"Unsupported change: {x.Reason}");
                    }
                })
                .Subscribe(() => { }, Log.HandleUiException)
                .AddTo(Anchors);

            source
                .ToObservableChangeSet()
                .WhenPropertyChanged(x => x.IsSelected)
                .Where(x => x.Value)
                .Subscribe(x =>
                {
                    SelectedValue = x.Sender;
                }, Log.HandleUiException)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.SelectedValue)
                .Subscribe(x =>
                {
                    SelectedItem = x == null ? null : FindItemByTab(x);
                    if (SelectedItem != null)
                    {
                        SelectedItem.IsSelected = true;
                    }
                })
                .AddTo(Anchors);
            
            source
                .ToObservableChangeSet()
                .WhenPropertyChanged(x => x.Path)
                .Subscribe(x =>
                {
                    var node = FindItemByTab(x.Sender);
                    if (node == null)
                    {
                        // node is already removed
                        return;
                    }
                    var directory = FindOrCreateByPath(node.Value.Path);
                    node.Parent = directory;
                }, Log.HandleUiException)
                .AddTo(Anchors);
        }

        public HolderTreeViewItemViewModel FindItemByTab(IAuraTabViewModel tab)
        {
            return root.FindChildren(x => x is HolderTreeViewItemViewModel holderNode && holderNode.Value == tab).Cast<HolderTreeViewItemViewModel>().SingleOrDefault();
        }
        
        private DirectoryTreeViewItemViewModel FindDirectoryByPath(string path)
        {
            return root.FindChildren(x => x is DirectoryTreeViewItemViewModel holderNode && holderNode.Path == path, root.Children).Cast<DirectoryTreeViewItemViewModel>().SingleOrDefault();
        }

        private DirectoryTreeViewItemViewModel FindOrCreateByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return root;
            }
            
            var split = path.Split(new [] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            var currentNode = root;
            foreach (var directoryName in split)
            {
                var directory =  currentNode.Children.OfType<DirectoryTreeViewItemViewModel>().FirstOrDefault(x => x.Name == directoryName);
                if (directory == null)
                {
                    var newDirectoryNode = new DirectoryTreeViewItemViewModel(currentNode)
                    {
                        Name = directoryName, 
                        Parent = currentNode 
                    };

                    currentNode = newDirectoryNode;
                }
                else
                {
                    currentNode = directory;
                }
            }

            return currentNode;
        }
    }
}