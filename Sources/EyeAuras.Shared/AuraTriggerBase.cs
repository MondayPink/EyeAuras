using ReactiveUI;
using System;
using System.Reactive.Linq;
using log4net;
using PoeShared.Scaffolding;
using ObservableEx = PoeShared.Scaffolding.ObservableEx;

namespace EyeAuras.Shared
{
    public abstract class AuraTriggerBase<TAuraProperties> : AuraModelBase<TAuraProperties>, IAuraTrigger where TAuraProperties : class, IAuraTriggerProperties, new()
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AuraTriggerBase<>));

        private bool isActive;
        private bool isInverted;
        private bool triggerValue;

        private TimeSpan activationTimeout;
        private DateTime? nextActivationTimestamp;
        private bool nextIsActiveValue;
        private bool enableAdvancedSettings;

        protected AuraTriggerBase()
        {
            //FIXME This class really starts to stink, double-value triggers kinda hard to debug
            this.WhenAnyValue(x => x.IsInverted)
                .Subscribe(x => RaisePropertyChanged(nameof(IsActive)))
                .AddTo(Anchors);

            var isActiveSource = this.WhenAnyValue(x => x.TriggerValue, x => x.IsInverted, x => x.EnableAdvancedSettings)
                .Select(x =>
                {
                    if (EnableAdvancedSettings)
                    {
                        return TriggerValue ^ IsInverted;
                    }

                    return TriggerValue;
                });
            
            this.WhenAnyValue(x => x.ActivationTimeout, x => x.EnableAdvancedSettings)
                .Select(() =>
                {
                    if (ActivationTimeout == default || !EnableAdvancedSettings)
                    {
                        NextActivationTimestamp = null;
                        return isActiveSource;
                    }

                    IsActive = false;
                    return isActiveSource.Do(x =>
                    {
                        NextActivationTimestamp = DateTime.Now + ActivationTimeout;
                        NextIsActiveValue = x;
                    }).Throttle(ActivationTimeout);
                })
                .Switch()
                .Subscribe(x => IsActive = x)
                .AddTo(Anchors);
            
            this.WhenAnyProperty(x => x.TimeLeftTillNextActivation)
                .Subscribe(() => RaisePropertyChanged(nameof(ActivationProgress)))
                .AddTo(Anchors);
            
            ObservableEx
                .BlockingTimer( TimeSpan.FromMilliseconds(250))
                .Subscribe(
                    () =>
                    {
                        RaisePropertyChanged(nameof(TimeLeftTillNextActivation));
                    })
                .AddTo(Anchors);

            this.WhenAnyProperty(x => x.IsInverted, x => x.ActivationTimeout)
                .Select(x => IsInverted || ActivationTimeout > TimeSpan.Zero)
                .Where(x => x)
                .Subscribe(x => EnableAdvancedSettings = x)
                .AddTo(Anchors);
        }

        public abstract string TriggerName { get; }

        public abstract string TriggerDescription { get; }

        public bool EnableAdvancedSettings
        {
            get => enableAdvancedSettings;
            set => RaiseAndSetIfChanged(ref enableAdvancedSettings, value);
        }
        
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
        
        public DateTime? NextActivationTimestamp
        {
            get => nextActivationTimestamp;
            private set => RaiseAndSetIfChanged(ref nextActivationTimestamp, value);
        }

        public bool NextIsActiveValue
        {
            get => nextIsActiveValue;
            private set => RaiseAndSetIfChanged(ref nextIsActiveValue, value);
        }

        public TimeSpan ActivationTimeout
        {
            get => activationTimeout;
            set => RaiseAndSetIfChanged(ref activationTimeout, value);
        }
        
        public TimeSpan? TimeLeftTillNextActivation => NextActivationTimestamp == null || NextActivationTimestamp < DateTime.Now || NextIsActiveValue == IsActive ? null : NextActivationTimestamp - DateTime.Now;
        
        public double ActivationProgress => TimeLeftTillNextActivation == null 
                ? 0
                : (ActivationTimeout.TotalMilliseconds - TimeLeftTillNextActivation.Value.TotalMilliseconds) / ActivationTimeout.TotalMilliseconds * 100;

        protected override void VisitLoad(TAuraProperties source)
        {
            IsInverted = source.IsInverted;
        }

        protected override void VisitSave(TAuraProperties source)
        {
            source.IsInverted = IsInverted;
        }

        public override string ToString()
        {
            return $"{TriggerName}: {IsActive}";
        }
    }
}