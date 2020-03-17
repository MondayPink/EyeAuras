using System;
using System.Threading;
using EyeAuras.DefaultAuras.Actions.PlaySound;
using EyeAuras.Shared;
using log4net;

namespace EyeAuras.DefaultAuras.Actions.Delay
{
    internal sealed class DelayAction : AuraActionBase<DelayActionProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PlaySoundAction));
        private TimeSpan delay;

        public DelayAction()
        {
        }

        public TimeSpan Delay
        {
            get => delay;
            set => this.RaiseAndSetIfChanged(ref delay, value);
        }

        protected override void Load(DelayActionProperties source)
        {
            Delay = source.Delay;
        }

        protected override DelayActionProperties Save()
        {
            return new DelayActionProperties()
            {
                Delay = delay
            };
        }

        public override string ActionName { get; } = "Delay";
        
        public override string ActionDescription { get; } = "Postpones next action for specified time";
        
        public override void Execute()
        {
            Log.Debug($"Delaying execution for {Delay}");
            Thread.Sleep(Delay);
        }
    }
}