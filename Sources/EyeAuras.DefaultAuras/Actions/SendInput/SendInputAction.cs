using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using WindowsInput;
using WindowsInput.Native;
using EyeAuras.Interception;
using EyeAuras.Shared;
using JetBrains.Annotations;
using log4net;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI.Hotkeys;

namespace EyeAuras.DefaultAuras.Actions.SendInput
{
    internal sealed class SendInputAction : AuraActionBase<SendInputProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SendInputAction));
        private static readonly TimeSpan DefaultModifierKeyStrokeDelay = TimeSpan.FromMilliseconds(100);

        private readonly IHotkeyConverter hotkeyConverter;
        private TimeSpan keyStrokeDelay;
        private HotkeyGesture hotkey;

        private IKeyboardSimulator keyboardSimulator;
        private bool isDriverBasedSimulator;

        public SendInputAction(
            [NotNull] IHotkeyConverter hotkeyConverter)
        {
            this.hotkeyConverter = hotkeyConverter;
            InstallDriverCommand = CommandWrapper.Create(InstallDriverCommandExecuted);
            UninstallDriverCommand = CommandWrapper.Create(UninstallDriverCommandExecuted);
            InitializeSimulator();
        }

        private void InitializeSimulator()
        {
            Log.Debug($"Initializing simulator, current state: {(IsDriverBasedSimulator ? "Interception-Based" : "InputSimulator")}");
            try
            {
                keyboardSimulator = new DriverBasedKeyboardSimulator();
                IsDriverBasedSimulator = true;
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load Interception driver-based keyboard simulator, falling back to {typeof(InputSimulator)}");
                keyboardSimulator = new InputSimulator().Keyboard;
                IsDriverBasedSimulator = false;
            }
            Log.Debug($"Initialized simulator, current state: {(IsDriverBasedSimulator ? "Interception-Based" : "InputSimulator")}");
        }

        private void InstallDriverCommandExecuted()
        {
            new DriverInstallHelper().Install();
            InitializeSimulator();
            MessageBox.Show($"Driver installation completed successfully. You must reboot for it to take effect.");
        }
        
        private void UninstallDriverCommandExecuted()
        {
            new DriverInstallHelper().Uninstall();
            InitializeSimulator();
        }

        public bool IsDriverBasedSimulator
        {
            get => isDriverBasedSimulator;
            set => this.RaiseAndSetIfChanged(ref isDriverBasedSimulator, value);
        }
        
        public ICommand InstallDriverCommand { get; }
        
        public ICommand UninstallDriverCommand { get; }

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

        protected override void VisitLoad(SendInputProperties source)
        {
            KeyStrokeDelay = source.KeyStrokeDelay;
            Hotkey = hotkeyConverter.ConvertFromString(source.Hotkey);
        }

        protected override void VisitSave(SendInputProperties source)
        {
            source.KeyStrokeDelay = KeyStrokeDelay;
            source.Hotkey = hotkeyConverter.ConvertToString(Hotkey);
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
                keyboardSimulator.Sleep(DefaultModifierKeyStrokeDelay);
            }
            if (Hotkey.ModifierKeys.HasFlag(ModifierKeys.Alt))
            {
                keyboardSimulator.KeyDown(VirtualKeyCode.MENU);
                keyboardSimulator.Sleep(DefaultModifierKeyStrokeDelay);
            }
            if (Hotkey.ModifierKeys.HasFlag(ModifierKeys.Shift))
            {
                keyboardSimulator.KeyDown(VirtualKeyCode.SHIFT);
                keyboardSimulator.Sleep(DefaultModifierKeyStrokeDelay);
            }

            keyboardSimulator.KeyDown(vk);
            keyboardSimulator.Sleep(KeyStrokeDelay);
            keyboardSimulator.KeyUp(vk);
            
            if (Hotkey.ModifierKeys.HasFlag(ModifierKeys.Control))
            {
                keyboardSimulator.KeyUp(VirtualKeyCode.CONTROL);
                keyboardSimulator.Sleep(DefaultModifierKeyStrokeDelay);
            }
            if (Hotkey.ModifierKeys.HasFlag(ModifierKeys.Alt))
            {
                keyboardSimulator.KeyUp(VirtualKeyCode.MENU);
                keyboardSimulator.Sleep(DefaultModifierKeyStrokeDelay);
            }
            if (Hotkey.ModifierKeys.HasFlag(ModifierKeys.Shift))
            {
                keyboardSimulator.KeyUp(VirtualKeyCode.SHIFT);
                keyboardSimulator.Sleep(DefaultModifierKeyStrokeDelay);
            }
        }
    }
}