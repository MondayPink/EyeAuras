using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace EyeAuras.UI.MainWindow.Models
{
    public interface IMainWindowBlocksRepository
    {
        ReadOnlyObservableCollection<object> StatusBarItems { [NotNull] get; }
    }
}