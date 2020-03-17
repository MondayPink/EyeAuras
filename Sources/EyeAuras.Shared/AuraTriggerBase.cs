using ReactiveUI;
using System;
using PoeShared.Scaffolding;

namespace EyeAuras.Shared
{
    public abstract class AuraTriggerBase<TAuraProperties> : AuraModelBase<TAuraProperties>, IAuraTrigger where TAuraProperties : class, IAuraTriggerProperties, new()
    {
        private bool isActive;
        private bool isInverted;

        protected AuraTriggerBase()
        {
            this.WhenAnyValue(x => x.IsInverted)
                .Subscribe(x => RaisePropertyChanged(nameof(IsActive)))
                .AddTo(Anchors);
        }

        public abstract string TriggerName { get; }

        public abstract string TriggerDescription { get; }
        
        public bool IsInverted    
        {
            get => isInverted;
            set => this.RaiseAndSetIfChanged(ref isInverted, value);
        }

        public bool IsActive
        {
            get => isActive ^ isInverted;
            set => RaiseAndSetIfChanged(ref isActive, value);
        }

        protected override void VisitLoad(TAuraProperties source)
        {
            IsInverted = source.IsInverted;
        }

        protected override void VisitSave(TAuraProperties source)
        {
            source.IsInverted = IsInverted;
        }
    }
}