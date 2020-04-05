using EyeAuras.Shared;
using EyeAuras.UI.Core.Services;
using log4net;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using EyeAuras.UI.Core.ViewModels;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Triggers.AuraIsActive
{
    internal sealed class AuraIsActiveTrigger : AuraTriggerBase<AuraIsActiveTriggerProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AuraIsActiveTrigger));

        private string auraId;
        private IEyeAuraViewModel aura;

        public AuraIsActiveTrigger([NotNull] ISharedContext sharedContext)
        {
            this.WhenAnyValue(x => x.AuraId)
                .Select(x => sharedContext.AuraList.FirstOrDefault(y => y.Id == AuraId))
                .Select(
                    x =>
                    {
                        if (x != null || string.IsNullOrEmpty(AuraId))
                        {
                            return Observable.Return(x);
                        }

                        return Observable.Merge(
                                sharedContext.AuraList.ToObservableChangeSet().SkipInitial().ToUnit(),
                                sharedContext.AuraList.ToObservableChangeSet().SkipInitial().WhenPropertyChanged(x => x.Id).ToUnit())
                            .Select(() => sharedContext.AuraList.FirstOrDefault(y => y.Id == AuraId))
                            .Where(x => x != null)
                            .Take(1);
                    })
                .Switch()
                .Subscribe(x => Aura = x, Log.HandleUiException)
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.Aura)
                .Select(_ => Aura == null 
                    ? Observable.Return(false) 
                    : Aura.WhenAnyValue(y => y.IsActive).Do(y => Log.Debug($"Child Aura {Aura.TabName}({Aura.Id}) IsActive changed to {Aura.IsActive}")))
                .Switch()
                .Subscribe(isActive => IsActive = isActive, Log.HandleUiException)
                .AddTo(Anchors);
        }

        public IEyeAuraViewModel Aura
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
        }

        protected override void VisitSave(AuraIsActiveTriggerProperties source)
        {
            source.AuraId = auraId;
            source.IsInverted = IsInverted;
            base.VisitSave(source);
        }
    }
}