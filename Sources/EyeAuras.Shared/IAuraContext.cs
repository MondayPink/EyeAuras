using System.Collections.Generic;
using System.ComponentModel;
using DynamicData;
using JetBrains.Annotations;

namespace EyeAuras.Shared
{
    public interface IAuraContext : INotifyPropertyChanged
    {
        string Id { [NotNull] get; }
        
        string Name { [NotNull] get; }
        
        bool IsActive { get; }
        
        IComplexAuraTrigger Triggers { [NotNull] get; }

        IComplexAuraAction OnEnterActions { [NotNull] get; }
        
        IComplexAuraAction WhileActiveActions { [NotNull] get; }

        IComplexAuraAction OnExitActions { [NotNull] get; }
        
        SourceCache<KeyValuePair<string, object>, string> Variables { [NotNull] get; }
        
        object this[string key] { [CanBeNull] get; [CanBeNull] set; }
    }
}