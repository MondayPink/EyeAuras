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

        public AuraIsActiveTrigger(
            [NotNull] ISharedContext sharedContext,
            [NotNull] IClock clock,
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

            this.WhenAnyValue(x => x.AuraTab, x => x.ActivationTimeout)
                .Select(_ => AuraTab == null
                    ? Observable.Return(false)
                    : AuraTab.WhenAnyValue(y => y.IsActive).Do(y =>
                        Log.Debug(
                            $"Source Aura {AuraTab.TabName}({AuraTab.Id}) IsActive changed to {AuraTab.IsActive}")))
                .Switch()
                .Subscribe(sourceIsActive =>
                {
                    Log.Debug($"AuraIsActive for aura {AuraTab} changed, IsActive = {sourceIsActive}, ActivationTimeout = {ActivationTimeout}");
                    TriggerValue = sourceIsActive;
                }, Log.HandleUiException)
                .AddTo(Anchors);
        }
        
        public IAuraViewModel AuraTab
        {
            get => aura;
            private set => this.RaiseAndSetIfChanged(ref aura, value);
        }

        [AuraProperty]
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
        }

        protected override void VisitSave(AuraIsActiveTriggerProperties source)
        {
            source.AuraId = AuraId;
            base.VisitSave(source);
        }

        public override string ToString()
        {
            return $"{base.ToString()} ({nameof(AuraId)}: {AuraId})";
        }
    }
}