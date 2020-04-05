using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using EyeAuras.DefaultAuras.Triggers.Default;
using EyeAuras.Shared;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity;

namespace EyeAuras.DefaultAuras.Triggers.Timer
{
    internal sealed class TimerTriggerEditor : AuraPropertiesEditorBase<TimerTrigger>
    {
        private readonly IClock clock;
        private readonly IScheduler bgScheduler;
        private readonly IScheduler uiScheduler;
        private readonly SerialDisposable activeSourceAnchors = new SerialDisposable();

        public TimerTriggerEditor(IClock clock, 
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            this.clock = clock;
            this.bgScheduler = bgScheduler;
            this.uiScheduler = uiScheduler;
            this.WhenAnyValue(x => x.Source)
                .Subscribe(HandleSourceChange)
                .AddTo(Anchors);

            this.WhenAnyProperty(x => x.TimeLeftTillNextActivation, x => x.TimeLeftTillNextDeactivation)
                .Subscribe(() => RaisePropertyChanged(nameof(ActivationProgress)))
                .AddTo(Anchors);
        }

        public int ActivationPeriod
        {
            get => (int)Source.ActivationPeriod.TotalMilliseconds;
            set => Source.ActivationPeriod = TimeSpan.FromMilliseconds(value);
        }
        
        public int DeactivationTimeout
        {
            get => (int)Source.DeactivationTimeout.TotalMilliseconds;
            set => Source.DeactivationTimeout = TimeSpan.FromMilliseconds(value);
        }

        public TimeSpan? TimeLeftTillNextActivation => Source == null ? TimeSpan.MaxValue : Source.NextActivationTimestamp - clock.Now;
        
        public TimeSpan? TimeLeftTillNextDeactivation => Source == null ? TimeSpan.MaxValue : Source.NextDeactivationTimestamp - clock.Now;

        public double ActivationProgress => Source == null 
            ? 0
            : TimeLeftTillNextActivation != null 
                ? (Source.ActivationPeriod.TotalMilliseconds - TimeLeftTillNextActivation.Value.TotalMilliseconds) / Source.ActivationPeriod.TotalMilliseconds * 100
                : TimeLeftTillNextDeactivation != null 
                    ? (TimeLeftTillNextDeactivation.Value.TotalMilliseconds) / Source.DeactivationTimeout.TotalMilliseconds * 100
                    : 0;
        
        private void HandleSourceChange()
        {
            var sourceAnchors = new CompositeDisposable().AssignTo(activeSourceAnchors);

            if (Source == null)
            {
                return;
            }

            Observable
                .Timer(DateTimeOffset.Now, TimeSpan.FromMilliseconds(250), bgScheduler)
                .ObserveOn(uiScheduler)
                .Subscribe(
                    () =>
                    {
                        RaisePropertyChanged(nameof(TimeLeftTillNextActivation));
                        RaisePropertyChanged(nameof(TimeLeftTillNextDeactivation));
                    })
                .AddTo(sourceAnchors);
        }
    }
}