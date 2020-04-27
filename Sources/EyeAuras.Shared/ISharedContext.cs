using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace EyeAuras.Shared
{
    public interface ISharedContext
    {
        ReadOnlyObservableCollection<IAuraViewModel> AuraList { [NotNull] get; }
    }
}