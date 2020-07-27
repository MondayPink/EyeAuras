using System;
using System.Threading;
using System.Threading.Tasks;
using EyeAuras.DefaultAuras.Actions.PlaySound;
using EyeAuras.Shared;
using log4net;
using PoeShared.Scaffolding;

namespace EyeAuras.DefaultAuras.Actions.Delay
{
    public sealed class DelayAction : AuraActionBase<DelayActionProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DelayAction));
        private TimeSpan delay;
        private CancellationTokenSource cancellationTokenSource;

        public DelayAction()
        {
            this.WhenAnyProperty(x => x.Properties)
                .Subscribe(() => cancellationTokenSource?.Cancel())
                .AddTo(Anchors);
        }

        [AuraProperty]
        public TimeSpan Delay
        {
            get => delay;
            set => this.RaiseAndSetIfChanged(ref delay, value);
        }

        protected override void VisitLoad(DelayActionProperties source)
        {
            Delay = source.Delay;
        }

        protected override void VisitSave(DelayActionProperties source)
        {
            source.Delay = delay;
        }

        public override string ActionName { get; } = "Delay";
        
        public override string ActionDescription { get; } = "Postpones next action for specified time";
        
        protected override void ExecuteInternal()
        {
            Log.Debug($"Delaying execution for {Delay}");
            cancellationTokenSource = new CancellationTokenSource();
            try
            {
                Task.Delay(Delay, cancellationTokenSource.Token).Wait();
            }
            catch (AggregateException)
            {
                Log.Debug($"Delay action was cancelled");
            }
        }
    }
}