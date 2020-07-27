using System.Collections.ObjectModel;
using System.ComponentModel;
using JetBrains.Annotations;

namespace EyeAuras.Shared
{
    public interface ISharedContext : INotifyPropertyChanged
    {
        ReadOnlyObservableCollection<IAuraViewModel> AuraList { [NotNull] get; }
    }
}