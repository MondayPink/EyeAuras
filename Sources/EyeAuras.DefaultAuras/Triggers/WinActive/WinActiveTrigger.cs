using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using EyeAuras.OnTopReplica;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using log4net;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace EyeAuras.DefaultAuras.Triggers.WinActive
{
    internal sealed class WinActiveTrigger : AuraTriggerBase<WinActiveTriggerProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WinActiveTrigger));
        private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;

        private readonly SerialDisposable activeTracker = new SerialDisposable();
        private readonly IWindowMatcher windowMatcher;
        private readonly IWindowListProvider windowListProvider;
        private readonly IFactory<WindowTracker, IStringMatcher> windowTrackerFactory;
        private WindowMatchParams targetWindow;

        public WinActiveTrigger(
            IWindowMatcher windowMatcher,
            IWindowListProvider windowListProvider,
            IFactory<WindowTracker, IStringMatcher> windowTrackerFactory)
        {
            this.windowMatcher = windowMatcher;
            this.windowListProvider = windowListProvider;
            this.windowTrackerFactory = windowTrackerFactory;
            activeTracker.AddTo(Anchors);

            this.WhenAnyValue(x => x.TargetWindow)
                .Subscribe(ApplyProperties)
                .AddTo(Anchors);
        }

        [AuraProperty]
        public WindowMatchParams TargetWindow
        {
            get => targetWindow;
            set => RaiseAndSetIfChanged(ref targetWindow, value);
        }

        public override string TriggerName { get; } = "Window Is Active";

        public override string TriggerDescription { get; } = "Checks whether window with specified title is active or not";

        private void ApplyProperties()
        {
            activeTracker.Disposable = null;

            if (TargetWindow.IsEmpty)
            {
                TriggerValue = false;
                return;
            }

            var matcher = new RegexStringMatcher().AddToWhitelist(Regex.Escape(TargetWindow.Title));
            var tracker = windowTrackerFactory.Create(matcher);

            activeTracker.Disposable = tracker
                .WhenAnyValue(x => x.ActiveProcessId)
                .Select(
                    () => new
                    {
                        tracker.IsActive,
                        tracker.ActiveWindowHandle,
                        tracker.ActiveProcessId,
                        CurrentProcessId
                    })
                .Do(x => Log.Debug($"WinActiveTrigger data updated(target: {TargetWindow}): {x}"))
                .Select(x => windowListProvider.ResolveByHandle(x.ActiveWindowHandle))
                .Select(x => new { Window = x, IsMatch = x != null && windowMatcher.IsMatch(x, targetWindow) })
                .Subscribe(x => TriggerValue = x.IsMatch);
        }

        protected override void VisitLoad(WinActiveTriggerProperties source)
        {
            base.VisitLoad(source);
            TargetWindow = source.WindowMatchParams;
        }

        protected override void VisitSave(WinActiveTriggerProperties source)
        {
            source.WindowMatchParams = TargetWindow;
            base.VisitSave(source);
        }
    }
}