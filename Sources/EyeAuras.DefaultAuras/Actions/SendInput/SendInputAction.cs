using System;
using System.Reactive.Linq;
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
using Unity;

namespace EyeAuras.DefaultAuras.Actions.SendInput
{
    internal sealed class SendInputAction : AuraActionBase<SendInputProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SendInputAction));
        private static readonly TimeSpan DefaultModifierKeyStrokeDelay = TimeSpan.FromMilliseconds(100);

        private readonly IHotkeyConverter hotkeyConverter;
        private readonly IInputSimulatorEx inputSimulator;

        private TimeSpan keyStrokeDelay;
        private HotkeyGesture hotkey;
        private bool isDriverBasedSimulator;
        private bool isUsb2KbdSimulator;
        private bool isWindowsSimulator;
        private string error;
        private bool isDriverInstalled;

        public SendInputAction(
            [Dependency(WellKnownKeyboardSimulators.InputSimulator)] IInputSimulatorEx windowsInputSimulator,
            [Dependency(WellKnownKeyboardSimulators.InterceptionDriver)] IInputSimulatorEx driverBasedSimulator,
            [Dependency(WellKnownKeyboardSimulators.Usb2Kbd)] IInputSimulatorEx usb2KbdSimulator,
            [NotNull] IHotkeyConverter hotkeyConverter)
        {
            this.hotkeyConverter = hotkeyConverter;
            IsDriverInstalled = driverBasedSimulator.IsAvailable;
            InstallDriverCommand = CommandWrapper.Create(InstallDriverCommandExecuted, Observable.Return(!IsDriverInstalled));
            UninstallDriverCommand = CommandWrapper.Create(UninstallDriverCommandExecuted,Observable.Return(IsDriverInstalled));
            
            Log.Debug($"Initializing simulator, usb2kbd.IsAvailable: {usb2KbdSimulator.IsAvailable}, driver.IsAvailable: {driverBasedSimulator.IsAvailable}, windows.IsAvailable: {windowsInputSimulator.IsAvailable}");

            if (usb2KbdSimulator.IsAvailable)
            {
                Log.Debug($"Using Usb2Kbd for input simulation {usb2KbdSimulator}");
                IsUsb2KbdSimulator = true;
                inputSimulator = usb2KbdSimulator;
            } else if (driverBasedSimulator.IsAvailable)
            {
                Log.Debug($"Using Keyboard driver for input simulation {driverBasedSimulator}");
                IsDriverBasedSimulator = true;
                inputSimulator = driverBasedSimulator;
            }
            else
            {
                Log.Debug($"Using InputSimulator for input simulation {windowsInputSimulator}");
                IsWindowsSimulator = true;
                inputSimulator = windowsInputSimulator;
            }
            Log.Debug($"Initialized simulator, current state: {(IsDriverBasedSimulator ? "Interception-Based" : "InputSimulator")}");
        }

        private void InstallDriverCommandExecuted()
        {
            new DriverInstallHelper().Install();
            MessageBox.Show($"Driver installation completed successfully. You must reboot for it to take effect.");
        }
        
        private void UninstallDriverCommandExecuted()
        {
            new DriverInstallHelper().Uninstall();
        }

        public string Error
        {
            get => error;
            set => this.RaiseAndSetIfChanged(ref error, value);
        }

        public bool IsDriverInstalled
        {
            get => isDriverInstalled;
            set => this.RaiseAndSetIfChanged(ref isDriverInstalled, value);
        }

        public bool IsDriverBasedSimulator
        {
            get => isDriverBasedSimulator;
            private set => this.RaiseAndSetIfChanged(ref isDriverBasedSimulator, value);
        }

        public bool IsUsb2KbdSimulator
        {
            get => isUsb2KbdSimulator;
            private set => this.RaiseAndSetIfChanged(ref isUsb2KbdSimulator, value);
        }

        public bool IsWindowsSimulator
        {
            get => isWindowsSimulator;
            private set => this.RaiseAndSetIfChanged(ref isWindowsSimulator, value);
        }
        
        public CommandWrapper InstallDriverCommand { get; }
        
        public CommandWrapper UninstallDriverCommand { get; }

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

            try
            {
                var vk = (VirtualKeyCode)KeyInterop.VirtualKeyFromKey(Hotkey.Key);
                if (Hotkey.ModifierKeys.HasFlag(ModifierKeys.Control))
                {
                    inputSimulator.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
                    inputSimulator.Keyboard.Sleep(DefaultModifierKeyStrokeDelay);
                }
                if (Hotkey.ModifierKeys.HasFlag(ModifierKeys.Alt))
                {
                    inputSimulator.Keyboard.KeyDown(VirtualKeyCode.MENU);
                    inputSimulator.Keyboard.Sleep(DefaultModifierKeyStrokeDelay);
                }
                if (Hotkey.ModifierKeys.HasFlag(ModifierKeys.Shift))
                {
                    inputSimulator.Keyboard.KeyDown(VirtualKeyCode.SHIFT);
                    inputSimulator.Keyboard.Sleep(DefaultModifierKeyStrokeDelay);
                }

                inputSimulator.Keyboard.KeyDown(vk);
                inputSimulator.Keyboard.Sleep(KeyStrokeDelay);
                inputSimulator.Keyboard.KeyUp(vk);
            
                if (Hotkey.ModifierKeys.HasFlag(ModifierKeys.Control))
                {
                    inputSimulator.Keyboard.KeyUp(VirtualKeyCode.CONTROL);
                    inputSimulator.Keyboard.Sleep(DefaultModifierKeyStrokeDelay);
                }
                if (Hotkey.ModifierKeys.HasFlag(ModifierKeys.Alt))
                {
                    inputSimulator.Keyboard.KeyUp(VirtualKeyCode.MENU);
                    inputSimulator.Keyboard.Sleep(DefaultModifierKeyStrokeDelay);
                }
                if (Hotkey.ModifierKeys.HasFlag(ModifierKeys.Shift))
                {
                    inputSimulator.Keyboard.KeyUp(VirtualKeyCode.SHIFT);
                    inputSimulator.Keyboard.Sleep(DefaultModifierKeyStrokeDelay);
                }
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to send input, hotkey: {hotkey}", e);
                Error = e.ToString();
            }
        }
    }
}