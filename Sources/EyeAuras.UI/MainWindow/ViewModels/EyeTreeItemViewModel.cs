using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using log4net;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI.TreeView;

namespace EyeAuras.UI.MainWindow.ViewModels
{
    internal abstract class EyeTreeItemViewModel : TreeViewItemViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(EyeTreeItemViewModel));

        private bool isFlipped;
        protected readonly Fallback<string> tabName = new Fallback<string>();

        protected EyeTreeItemViewModel(EyeTreeItemViewModel parent) : base(parent)
        {
            RenameCommand = CommandWrapper.Create<string>(RenameCommandExecuted);

            this.RaiseWhenSourceValue(x => x.Name, tabName, x => x.Value).AddTo(Anchors);
            this.RaiseWhenSourceValue(x => x.DefaultName, tabName, x => x.DefaultValue).AddTo(Anchors);
            
            CollapseAllCommand = CommandWrapper.Create(CollapseAllCommandExecuted);
            ExpandAllCommand = CommandWrapper.Create(ExpandAllCommandExecuted);
        }

        private void ExpandAllCommandExecuted()
        {
            IsExpanded = true;
            this.FindChildrenOfType<IDirectoryTreeViewItemViewModel>().ForEach(x => x.IsExpanded = true);
        }

        private void CollapseAllCommandExecuted()
        {
            IsExpanded = false;
            this.FindChildrenOfType<IDirectoryTreeViewItemViewModel>().ForEach(x => x.IsExpanded = false);
        }

        public string DefaultName => tabName.DefaultValue;

        public string Name
        {
            get => tabName.Value;
            set => tabName.SetValue(value);
        }

        public CommandWrapper RenameCommand { [NotNull] get; }
        
        public CommandWrapper CollapseAllCommand { [NotNull] get; }
        
        public CommandWrapper ExpandAllCommand { [NotNull] get; }

        public bool IsFlipped
        {
            get => isFlipped;
            set => RaiseAndSetIfChanged(ref isFlipped, value);
        }

        public static ITreeViewItemViewModel FindRoot(ITreeViewItemViewModel node)
        {
            var result = node;
            while (result.Parent != null)
            {
                result = result.Parent;
            }
            return result;
        } 
        
        public static string FindPath(ITreeViewItemViewModel node)
        {
            var result = new StringBuilder();

            while (node != null)
            {
                if (node is DirectoryTreeViewItemViewModel parentDir)
                {
                    result.Insert(0, parentDir.Name + Path.DirectorySeparatorChar);
                }

                node = node.Parent;
            }
            return result.ToString().Trim(Path.DirectorySeparatorChar);
        }
         
        private void RenameCommandExecuted(string value)
        {
            if (IsFlipped)
            {
                if (value == null)
                {
                    // Cancel
                }
                else if (string.IsNullOrWhiteSpace(value))
                {
                    RenameTabTo(default);
                }
                else
                {
                    RenameTabTo(value);
                }
            }
            else
            {
                var root = FindRoot(this);
                var allFlipped = root.FindChildrenOfType<EyeTreeItemViewModel>().Where(x => x.IsFlipped && x != this)
                    .ToArray();
                allFlipped.ForEach(x => x.IsFlipped = false);
            }

            IsFlipped = !IsFlipped;
        }

        private void RenameTabTo(string newTabNameOrDefault)
        {
            if (newTabNameOrDefault == tabName.Value)
            {
                return;
            }

            var previousValue = tabName.Value;
            tabName.SetValue(newTabNameOrDefault);
            Log.Debug($"[{Name}] Changed name of tab {tabName.DefaultValue}, {previousValue} => {tabName.Value}");
        }
    }
}