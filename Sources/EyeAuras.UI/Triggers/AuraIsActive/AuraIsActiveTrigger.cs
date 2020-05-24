using EyeAuras.Shared;
using log4net;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity;

namespace EyeAuras.UI.Triggers.AuraIsActive
{
    internal sealed class AuraIsActiveTrigger : AuraTriggerBase<AuraIsActiveTriggerProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AuraIsActiveTrigger));

        private string auraId;
        private IAuraViewModel aura;
        private TimeSpan activationTimeout;
        private DateTime? nextActivationTimestamp;
        private bool nextIsActiveValue;

        public AuraIsActiveTrigger(
            [NotNull] ISharedContext sharedContext,
            IClock clock,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            this.WhenAnyValue(x => x.AuraId)
                .Select(x => sharedContext.AuraList.FirstOrDefault(y => y.Id == AuraId))
                .Select(
                    x =>
                    {
                        if (x != null)
                        {
                            return Observable.Return(x);
                        }
                        if (string.IsNullOrEmpty(AuraId))
                        {
                            return Observable.Empty<IAuraViewModel>();
                        }

                        return Observable.Merge(
                                sharedContext.AuraList.ToObservableChangeSet().SkipInitial().ToUnit())
                            .StartWithDefault()
                            .Select(() => sharedContext.AuraList.FirstOrDefault(y => y.Id == AuraId))
                            .Where(x => x != null)
                            .Take(1);
                    })
                .Switch()
                .Do(y => Log.Debug(y != null ? $"Aura {y.TabName}({y.Id}) is selected as Source, isActive{y.IsActive}" : $"Source aura selection is reset to null, auraId {AuraId}"))
                .Subscribe(x => AuraTab = x, Log.HandleUiException)
                .AddTo(Anchors);
            
            var isActiveSource = this.WhenAnyValue(x => x.AuraTab, x => x.ActivationTimeout)
                .Select(_ => AuraTab == null 
                    ? Observable.Return(false) 
                    : AuraTab.WhenAnyValue(y => y.IsActive).Do(y => Log.Debug($"Source Aura {AuraTab.TabName}({AuraTab.Id}) IsActive changed to {AuraTab.IsActive}")))
                .Switch();
                
            this.WhenAnyValue(x => x.ActivationTimeout, x => x.IsInverted)
                .Select(() =>
                {
                    if (ActivationTimeout == default)
                    {
                        NextActivationTimestamp = null;
                        return isActiveSource;
                    }

                    TriggerValue = false;
                    return isActiveSource.Do(x =>
                    {
                        NextActivationTimestamp = clock.Now + ActivationTimeout;
                        NextIsActiveValue = x;
                    }).Throttle(ActivationTimeout, bgScheduler);
                })
                .Switch()
                .Subscribe(sourceIsActive =>
                {
                    Log.Info($"AuraIsActive for aura {AuraTab} changed, IsActive = {sourceIsActive}, ActivationTimeout = {ActivationTimeout}");
                    TriggerValue = sourceIsActive;
                }, Log.HandleUiException)
                .AddTo(Anchors);
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
        
        public IAuraViewModel AuraTab
        {
            get => aura;
            private set => this.RaiseAndSetIfChanged(ref aura, value);
        }

        public string AuraId    
        {
            get => auraId;
            set => this.RaiseAndSetIfChanged(ref auraId, value);
        }

        public override string TriggerName { get; } = "Aura Is Active";

        public override string TriggerDescription { get; } = "Checks whether specified Aura is active or not";

        protected override void VisitLoad(AuraIsActiveTriggerProperties source)
        {
            base.VisitLoad(source);
            AuraId = source.AuraId;
            ActivationTimeout = source.ActivationTimeout;
        }

        protected override void VisitSave(AuraIsActiveTriggerProperties source)
        {
            source.AuraId = AuraId;
            source.ActivationTimeout = ActivationTimeout;
            base.VisitSave(source);
        }
    }
}