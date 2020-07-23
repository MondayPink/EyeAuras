using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using EyeAuras.DefaultAuras.Triggers.Default;
using EyeAuras.Shared;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity;

namespace EyeAuras.DefaultAuras.Triggers.Timer
{
    public sealed class TimerTrigger : AuraTriggerBase<TimerTriggerProperties>
    {
        private static readonly TimeSpan TimerResolution = TimeSpan.FromMilliseconds(500);
        private readonly IClock clock;
        private TimeSpan deactivationTimeout;
        private TimeSpan activationPeriod;
        private DateTime? nextActivationTimestamp;
        private DateTime? nextDeactivationTimestamp;

        public TimerTrigger(
            IClock clock,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            this.clock = clock;
            this.WhenAnyProperty(x => x.DeactivationTimeout, x => x.ActivationPeriod)
                .Select(
                    x =>
                    {
                        TriggerValue = false;
                        NextActivationTimestamp = clock.Now + ActualActivationPeriod;
                        NextDeactivationTimestamp = null;
                        return Observable
                            .Timer(ActualActivationPeriod, bgScheduler)
                            .Do(
                                _ =>
                                {
                                    TriggerValue = true;
                                    NextActivationTimestamp = null;
                                    NextDeactivationTimestamp = clock.Now + DeactivationTimeout;
                                })
                            .Select(x => DeactivationTimeout > TimeSpan.Zero ? Observable.Timer(DeactivationTimeout, bgScheduler).ToUnit() : Observable.Return(Unit.Default))
                            .Switch()
                            .Do(
                                _ =>
                                {
                                    TriggerValue = false;
                                    NextActivationTimestamp = clock.Now + ActualActivationPeriod;
                                    NextDeactivationTimestamp = null;
                                })
                            .Repeat();
                    })
                .Switch()
                .Subscribe()
                .AddTo(Anchors);
        }

        public override string TriggerName { get; } = "Timer Trigger";

        public override string TriggerDescription { get; } = "Trigger that switches to True on set interval";

        public DateTime? NextActivationTimestamp
        {
            get => nextActivationTimestamp;
            set => this.RaiseAndSetIfChanged(ref nextActivationTimestamp, value);
        }

        public DateTime? NextDeactivationTimestamp
        {
            get => nextDeactivationTimestamp;
            set => this.RaiseAndSetIfChanged(ref nextDeactivationTimestamp, value);
        }

        [AuraProperty]
        public TimeSpan DeactivationTimeout
        {
            get => deactivationTimeout;
            set => this.RaiseAndSetIfChanged(ref deactivationTimeout, value);
        }

        [AuraProperty]
        public TimeSpan ActivationPeriod
        {
            get => activationPeriod;
            set => this.RaiseAndSetIfChanged(ref activationPeriod, value);
        }

        public TimeSpan ActualActivationPeriod => ActivationPeriod < TimerResolution
            ? TimerResolution
            : ActivationPeriod;

        protected override void VisitLoad(TimerTriggerProperties source)
        {
            base.VisitLoad(source);
            ActivationPeriod = source.ActivationPeriod;
            DeactivationTimeout = source.DeactivationTimeout;
        }

        protected override void VisitSave(TimerTriggerProperties source)
        {
            base.VisitSave(source);
            source.ActivationPeriod = ActivationPeriod;
            source.DeactivationTimeout = DeactivationTimeout;
        }
    }
}