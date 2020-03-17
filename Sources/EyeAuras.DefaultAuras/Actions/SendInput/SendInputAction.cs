using System;
using System.Threading;
using System.Windows.Input;
using WindowsInput;
using WindowsInput.Native;
using EyeAuras.Shared;
using log4net;
using PoeShared.UI.Hotkeys;

namespace EyeAuras.DefaultAuras.Actions.SendInput
{
    internal sealed class SendInputAction : AuraActionBase<SendInputProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SendInputAction));
        private TimeSpan keyStrokeDelay;
        private HotkeyGesture hotkey;
        private static readonly TimeSpan DefaultModifierKeyStrokeDelay = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan DefaultKeyStrokeDelay = TimeSpan.FromMilliseconds(500);
        
        private readonly IKeyboardSimulator keyboardSimulator = new InputSimulator().Keyboard;

        public TimeSpan KeyStrokeDelay
        {
            get => keyStrokeDelay;
            set => this.RaiseAndSetIfChanged(ref keyStrokeDelay, value);
        }
        
        public HotkeyGesture Hotkey
        {
            get => hotkey;
            set => RaiseAndSetIfChanged(ref hotkey, value);
        }

        protected override void Load(SendInputProperties source)
        {
        }

        protected override SendInputProperties Save()
        {
            return new SendInputProperties()
            {
            };
        }

        public override string ActionName { get; } = "Send Input";
        
        public override string ActionDescription { get; } = "Send input via keyboard emulation";
        
        public override void Execute()
        {
            if (Hotkey == null || Hotkey.IsEmpty)
            {
                return;
            }
            var vk = (VirtualKeyCode)KeyInterop.VirtualKeyFromKey(Hotkey.Key);
            if (Hotkey.ModifierKeys.HasFlag(ModifierKeys.Control))
            {
                keyboardSimulator.KeyDown(VirtualKeyCode.CONTROL);
                Thread.Sleep(DefaultModifierKeyStrokeDelay);
            }
            if (Hotkey.ModifierKeys.HasFlag(ModifierKeys.Alt))
            {
                keyboardSimulator.KeyDown(VirtualKeyCode.MENU);
                Thread.Sleep(DefaultModifierKeyStrokeDelay);
            }
            if (Hotkey.ModifierKeys.HasFlag(ModifierKeys.Shift))
            {
                keyboardSimulator.KeyDown(VirtualKeyCode.SHIFT);
                Thread.Sleep(DefaultModifierKeyStrokeDelay);
            }
            keyboardSimulator.KeyDown(vk);
            Thread.Sleep(keyStrokeDelay <= TimeSpan.Zero ? DefaultKeyStrokeDelay : keyStrokeDelay);
            keyboardSimulator.KeyUp(vk);
            if (Hotkey.ModifierKeys.HasFlag(ModifierKeys.Control))
            {
                keyboardSimulator.KeyUp(VirtualKeyCode.CONTROL);
                Thread.Sleep(DefaultModifierKeyStrokeDelay);
            }
            if (Hotkey.ModifierKeys.HasFlag(ModifierKeys.Alt))
            {
                keyboardSimulator.KeyUp(VirtualKeyCode.MENU);
                Thread.Sleep(DefaultModifierKeyStrokeDelay);
            }
            if (Hotkey.ModifierKeys.HasFlag(ModifierKeys.Shift))
            {
                keyboardSimulator.KeyUp(VirtualKeyCode.SHIFT);
                Thread.Sleep(DefaultModifierKeyStrokeDelay);
            }
        }
    }
}