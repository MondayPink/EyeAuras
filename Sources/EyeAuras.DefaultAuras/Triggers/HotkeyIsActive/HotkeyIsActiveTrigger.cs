using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using EyeAuras.Shared;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.UI.Hotkeys;
using ReactiveUI;
using Unity;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace EyeAuras.DefaultAuras.Triggers.HotkeyIsActive
{
    public sealed class HotkeyIsActiveTrigger : AuraTriggerBase<HotkeyIsActiveProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HotkeyIsActiveTrigger));
        private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;
        private readonly IHotkeyConverter hotkeyConverter;

        private HotkeyGesture hotkey;
        private HotkeyMode hotkeyMode;
        private bool suppressKey;

        public HotkeyIsActiveTrigger(
            [NotNull] IHotkeyConverter hotkeyConverter,
            [NotNull] IKeyboardEventsSource eventSource,
            [NotNull] [Dependency(WellKnownWindows.MainWindow)] IWindowTracker mainWindowTracker,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            this.hotkeyConverter = hotkeyConverter;
            Disposable
                .Create(() => Log.Debug($"Disposing HotkeyTrigger, gesture: {Hotkey} (mode: {HotkeyMode})"))
                .AddTo(Anchors);
            base.TriggerValue = false;
            this.RaiseWhenSourceValue(x => x.Properties, this, x => x.IsActive).AddTo(Anchors);

            this.WhenAnyValue(x => x.Hotkey)
                .Select(hotkey => hotkey == null ? Observable.Empty<HotkeyData>() : BuildHotkeySubscription(eventSource))
                .Switch()
                .DistinctUntilChanged(x => new { x.Hotkey, x.KeyDown })
                .Where(
                    hotkeyData =>
                    {
                        /*
                         * This method MUST be executed on the same thread which emitted Key/Mouse event
                         * otherwise .Handled value will be ignored due to obvious concurrency reasons
                         */
                        if (mainWindowTracker.ActiveProcessId != CurrentProcessId)
                        {
                            Log.Debug($"Application is NOT active, processing hotkey {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}, suppressKey: {suppressKey},  configuredKey: {Hotkey}, mode: {HotkeyMode})");
                            if (suppressKey)
                            {
                                hotkeyData.MarkAsHandled();
                            }
                            return true;
                        }

                        Log.Debug($"Application is active, skipping hotkey {hotkeyData.Hotkey} (isDown: {hotkeyData.KeyDown}, suppressKey: {suppressKey},  configuredKey: {Hotkey}, mode: {HotkeyMode})");
                        return false;
                    })
                .Subscribe(
                    hotkeyData =>
                    {
                        Log.Debug($"Hotkey {hotkeyData.Hotkey} pressed, state: {(hotkeyData.KeyDown ? "down" : "up")}, suppressed: {suppressKey}");

                        if (HotkeyMode == HotkeyMode.Click)
                        {
                            if (!hotkeyData.KeyDown)
                            {
                                base.TriggerValue = !base.TriggerValue;
                            }
                        }
                        else
                        {
                            base.TriggerValue = !base.TriggerValue;
                        }
                    },
                    Log.HandleUiException)
                .AddTo(Anchors);
        }

        public HotkeyGesture Hotkey
        {
            get => hotkey;
            set => RaiseAndSetIfChanged(ref hotkey, value);
        }

        public HotkeyMode HotkeyMode
        {
            get => hotkeyMode;
            set => RaiseAndSetIfChanged(ref hotkeyMode, value);
        }

        public bool SuppressKey
        {
            get => suppressKey;
            set => RaiseAndSetIfChanged(ref suppressKey, value);
        }

        public override string TriggerName { get; } = "Hotkey Is Active";

        public override string TriggerDescription { get; } = "Checks that hotkey is Held or Clicked";

        protected override void VisitLoad(HotkeyIsActiveProperties source)
        {
            base.VisitLoad(source);
            HotkeyMode = source.HotkeyMode;
            Hotkey = hotkeyConverter.ConvertFromString(source.Hotkey);
            SuppressKey = source.SuppressKey;
        }

        protected override void VisitSave(HotkeyIsActiveProperties source)
        {
            base.VisitSave(source);
            source.HotkeyMode = hotkeyMode;
            source.Hotkey = hotkeyConverter.ConvertToString(hotkey);
            source.SuppressKey = suppressKey;
        }

        private bool IsConfiguredHotkey(HotkeyData data)
        {
            if (data.Hotkey == null || hotkey == null)
            {
                return false;
            }

            if (data.Hotkey.Key == Key.None && data.Hotkey.MouseButton == null)
            {
                return false;
            }

            var pressedHotkey = data.Hotkey.ToString();
            var result = pressedHotkey.Equals(hotkey.ToString());

            return result;
        }

        private IObservable<HotkeyData> BuildHotkeySubscription(
            IKeyboardEventsSource eventSource)
        {
            var hotkeyDown =
                Observable.Merge(eventSource.WhenKeyDown.Select(HotkeyData.FromEvent))
                    .Where(IsConfiguredHotkey)
                    .Select(x => x.SetKeyDown(true));
            
            var hotkeyUp =
                Observable.Merge(eventSource.WhenKeyUp.Select(HotkeyData.FromEvent))
                    .Where(IsConfiguredHotkey)
                    .Select(x => x.SetKeyDown(false));
            

            return hotkeyDown
                .Merge(hotkeyUp);
        }

        private struct HotkeyData
        {
            public KeyEventArgs KeyEventArgs { get; set; }

            public MouseEventArgs MouseEventArgs { get; set; }

            public HotkeyGesture Hotkey { get; set; }

            public bool KeyDown { get; set; }

            public HotkeyData SetKeyDown(bool value)
            {
                KeyDown = value;
                return this;
            }

            public HotkeyData MarkAsHandled()
            {
                if (KeyEventArgs != null)
                {
                    KeyEventArgs.Handled = true;
                }

                return this;
            }

            public static HotkeyData FromEvent(MouseEventArgs args)
            {
                return new HotkeyData
                {
                    Hotkey = new HotkeyGesture(args.Button),
                    MouseEventArgs = args
                };
            }

            public static HotkeyData FromEvent(KeyEventArgs args)
            {
                return new HotkeyData
                {
                    Hotkey = new HotkeyGesture(args.KeyCode.ToInputKey(), args.Modifiers.ToModifiers()),
                    KeyEventArgs = args
                };
            }
        }
    }
}