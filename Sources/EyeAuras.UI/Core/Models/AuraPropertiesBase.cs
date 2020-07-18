using System;
using System.Collections.Generic;
using EyeAuras.Shared;
using JetBrains.Annotations;
using PoeShared.Modularity;

namespace EyeAuras.UI.Core.Models
{
    internal abstract class AuraPropertiesBase : IAuraProperties
    {
        public string Id { get; set; }

        public string Name { get; set; }
        
        public string Path { get; set; }

        public bool IsEnabled { get; set; } = true;
        
        public IList<PoeConfigMetadata<IAuraProperties>> TriggerProperties { [CanBeNull] get; [CanBeNull] set; } =
            new List<PoeConfigMetadata<IAuraProperties>>();

        public IList<PoeConfigMetadata<IAuraProperties>> OnEnterActionProperties { [CanBeNull] get; [CanBeNull] set; } =
            new List<PoeConfigMetadata<IAuraProperties>>();
        
        public IList<PoeConfigMetadata<IAuraProperties>> WhileActiveActionProperties { [CanBeNull] get; [CanBeNull] set; } =
            new List<PoeConfigMetadata<IAuraProperties>>();
        
        public IList<PoeConfigMetadata<IAuraProperties>> OnExitActionProperties { [CanBeNull] get; [CanBeNull] set; } =
            new List<PoeConfigMetadata<IAuraProperties>>();
        
        public TimeSpan WhileActiveActionsTimeout { get; set; }
        
        public IAuraProperties CoreProperties { get; set; }

        public virtual bool IsValid => !string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(Name);

        public abstract int Version { get; set; }
    }
}