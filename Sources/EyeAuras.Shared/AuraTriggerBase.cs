using ReactiveUI;
using System;
using PoeShared.Scaffolding;

namespace EyeAuras.Shared
{
    public abstract class AuraTriggerBase<TAuraProperties> : AuraModelBase<TAuraProperties>, IAuraTrigger where TAuraProperties : class, IAuraTriggerProperties, new()
    {
        private bool isActive;
        private bool isInverted;
        private bool triggerValue;

        protected AuraTriggerBase()
        {
            this.WhenAnyValue(x => x.IsInverted)
                .Subscribe(x => RaisePropertyChanged(nameof(IsActive)))
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.TriggerValue, x => x.IsInverted)
                .Subscribe(() => IsActive = TriggerValue ^ isInverted)
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
            get => isActive;
            private set => RaiseAndSetIfChanged(ref isActive, value);
        }

        public bool TriggerValue
        {
            get => triggerValue;
            set => RaiseAndSetIfChanged(ref triggerValue, value);
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