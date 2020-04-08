using System;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using log4net;
using PoeShared;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace EyeAuras.DefaultAuras.Actions.WinActivate
{
    internal sealed class WinActivateAction : AuraActionBase<WinActivateActionProperties>
    {
        private readonly IClock clock;
        private static readonly ILog Log = LogManager.GetLogger(typeof(WinActivateAction));
        private TimeSpan timeout;

        public WinActivateAction(
            IClock clock,
            IWindowSelectorViewModel windowSelector)
        {
            this.clock = clock;
            WindowSelector = windowSelector;
            this.RaiseWhenSourceValue(x => x.TargetWindow, windowSelector, x => x.TargetWindow).AddTo(Anchors);
        }

        public IWindowSelectorViewModel WindowSelector { get; }

        public WindowMatchParams TargetWindow
        {
            get => WindowSelector.TargetWindow;
            set => WindowSelector.TargetWindow = value;
        }

        public TimeSpan Timeout
        {
            get => timeout;
            set => this.RaiseAndSetIfChanged(ref timeout, value);
        }

        public override string ActionName { get; } = "Win Activate";

        public override string ActionDescription { get; } = "activates selected window";

        protected override void VisitLoad(WinActivateActionProperties source)
        {
            TargetWindow = source.WindowMatchParams;
        }

        protected override void VisitSave(WinActivateActionProperties source)
        {
            source.WindowMatchParams = TargetWindow;
        }

        public override void Execute()
        {
            var activeWindow = WindowSelector.ActiveWindow;
            if (activeWindow == null)
            {
                return;
            }

            if (activeWindow.Handle == UnsafeNative.GetForegroundWindow())
            {
                return;
            }

            Log.Debug($"Bringing window {activeWindow} to foreground");
            UnsafeNative.SetForegroundWindow(activeWindow.Handle);

            var now = clock.Now;
            while (UnsafeNative.GetForegroundWindow() != activeWindow.Handle)
            {
                if (timeout <= TimeSpan.Zero)
                {
                    break;
                }
                if (clock.Now - now > timeout)
                {
                    Log.Warn($"Failed to switch to window ${activeWindow.Handle.ToHexadecimal()}");
                    break;
                }
            }
        }
    }
}