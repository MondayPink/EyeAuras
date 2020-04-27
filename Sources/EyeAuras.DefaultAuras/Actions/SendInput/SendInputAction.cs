using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using WindowsInput;
using WindowsInput.Native;
using EyeAuras.DefaultAuras.Actions.WinActivate;
using EyeAuras.Interception;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.Services;
using PoeShared.UI.Hotkeys;
using ReactiveUI;
using Unity;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using MouseButton = System.Windows.Input.MouseButton;
using Point = System.Drawing.Point;

namespace EyeAuras.DefaultAuras.Actions.SendInput
{
    internal sealed class SendInputAction : AuraActionBase<SendInputProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SendInputAction));
        private static readonly TimeSpan DefaultModifierKeyStrokeDelay = TimeSpan.FromMilliseconds(100);

        private readonly IUserInputBlocker userInputBlocker;
        private readonly IHotkeyConverter hotkeyConverter;
        private readonly WinActivateAction winActivateAction;

        private TimeSpan keyStrokeDelay;
        private HotkeyGesture hotkey;
        private bool isDriverBasedSimulator;
        private bool isUsb2KbdSimulator;
        private bool isWindowsSimulator;
        private bool isDriverInstalled;
        private Point mouseLocation;
        private IInputSimulatorEx inputSimulator;
        private bool restoreMousePosition;
        private bool blockUserInput;

        public SendInputAction(
            IUserInputBlocker userInputBlocker,
            IWindowSelectorViewModel windowSelector,
            IFactory<WinActivateAction, IWindowSelectorViewModel> winActivateActionFactory,
            [Dependency(WellKnownKeyboardSimulators.InputSimulator)] IInputSimulatorEx windowsInputSimulator,
            [Dependency(WellKnownKeyboardSimulators.InterceptionDriver)] IInputSimulatorEx driverBasedSimulator,
            [Dependency(WellKnownKeyboardSimulators.Usb2Kbd)] IInputSimulatorEx usb2KbdSimulator,
            IHotkeyConverter hotkeyConverter)
        {
            WindowSelector = windowSelector;
            winActivateAction = winActivateActionFactory.Create(windowSelector).AddTo(Anchors);
            this.userInputBlocker = userInputBlocker;
            this.hotkeyConverter = hotkeyConverter;
            IsDriverInstalled = driverBasedSimulator.IsAvailable;
            InstallDriverCommand = CommandWrapper.Create(InstallDriverCommandExecuted, Observable.Return(!IsDriverInstalled));
            UninstallDriverCommand = CommandWrapper.Create(UninstallDriverCommandExecuted,Observable.Return(IsDriverInstalled));
            
            this.RaiseWhenSourceValue(x => x.IsMouseGesture, this, x => x.Hotkey).AddTo(Anchors);
            this.RaiseWhenSourceValue(x => x.IsKeyboardGesture, this, x => x.Hotkey).AddTo(Anchors);

            Observable.CombineLatest(
                    usb2KbdSimulator.WhenAnyValue(x => x.IsAvailable).ToUnit(),
                    driverBasedSimulator.WhenAnyValue(x => x.IsAvailable).ToUnit(),
                    windowsInputSimulator.WhenAnyValue(x => x.IsAvailable).ToUnit())
                .StartWithDefault()
                .Subscribe(
                    () =>
                    {
                        Log.Debug($"Initializing simulator, usb2kbd.IsAvailable: {usb2KbdSimulator.IsAvailable}, driver.IsAvailable: {driverBasedSimulator.IsAvailable}, windows.IsAvailable: {windowsInputSimulator.IsAvailable}");

                        if (usb2KbdSimulator.IsAvailable)
                        {
                            Log.Debug($"Using Usb2Kbd for input simulation {usb2KbdSimulator}");
                            IsUsb2KbdSimulator = true;
                            IsDriverBasedSimulator = false;
                            IsWindowsSimulator = false;
                            InputSimulator = usb2KbdSimulator;
                        } else if (driverBasedSimulator.IsAvailable)
                        {
                            Log.Debug($"Using Keyboard driver for input simulation {driverBasedSimulator}");
                            IsDriverBasedSimulator = true;
                            IsWindowsSimulator = false;
                            IsUsb2KbdSimulator = false;
                            InputSimulator = driverBasedSimulator;
                        }
                        else
                        {
                            Log.Debug($"Using InputSimulator for input simulation {windowsInputSimulator}");
                            IsWindowsSimulator = true;
                            IsDriverBasedSimulator = false;
                            IsUsb2KbdSimulator = false;
                            InputSimulator = windowsInputSimulator;
                        }
                        Log.Debug($"Initialized simulator, current state: {(IsDriverBasedSimulator ? "Interception-Based" : "InputSimulator")}");
                    }, Log.HandleUiException)
                .AddTo(Anchors);
        }
        
        [ComparisonIgnore]
        public IWindowSelectorViewModel WindowSelector { get; }

        public bool BlockUserInput
        {
            get => blockUserInput;
            set => this.RaiseAndSetIfChanged(ref blockUserInput, value);
        }

        public bool IsDriverInstalled
        {
            get => isDriverInstalled;
            private set => this.RaiseAndSetIfChanged(ref isDriverInstalled, value);
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

        public bool IsMouseGesture => Hotkey?.MouseButton != null;
        
        public bool IsKeyboardGesture => Hotkey?.Key != Key.None;
        
        public CommandWrapper InstallDriverCommand { get; }
        
        public CommandWrapper UninstallDriverCommand { get; }
        
        public bool RestoreMousePosition
        {
            get => restoreMousePosition;
            set => this.RaiseAndSetIfChanged(ref restoreMousePosition, value);
        }
        
        public Point MouseLocation
        {
            get => mouseLocation;
            set => this.RaiseAndSetIfChanged(ref mouseLocation, value);
        }

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

        public IInputSimulatorEx InputSimulator
        {
            get => inputSimulator;
            private set => this.RaiseAndSetIfChanged(ref inputSimulator, value);
        }
        
        private void InstallDriverCommandExecuted()
        {
            new DriverInstallHelper().Install();
            MessageBox.Show($"Driver installation completed successfully. You must reboot for it to take effect.");
        }
        
        private void UninstallDriverCommandExecuted()
        {
            if (MessageBox.Show(
                    Application.Current.MainWindow,
                    "Are you sure you want to uninstall virtual keyboard/mouse driver ?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No) !=
                MessageBoxResult.Yes)
            {
                return;
            }
            new DriverInstallHelper().Uninstall();
        }

        protected override void VisitLoad(SendInputProperties source)
        {
            KeyStrokeDelay = source.KeyStrokeDelay;
            Hotkey = hotkeyConverter.ConvertFromString(source.Hotkey);
            WindowSelector.TargetWindow = source.TargetWindow;
            MouseLocation = source.MouseLocation;
            RestoreMousePosition = source.RestoreMousePosition;
            BlockUserInput = source.BlockUserInput;
        }

        protected override void VisitSave(SendInputProperties source)
        {
            source.KeyStrokeDelay = KeyStrokeDelay;
            source.Hotkey = hotkeyConverter.ConvertToString(Hotkey);
            source.TargetWindow = WindowSelector.TargetWindow;
            source.RestoreMousePosition = RestoreMousePosition;
            source.MouseLocation = MouseLocation;
            source.BlockUserInput = BlockUserInput;
        }

        public override string ActionName { get; } = "Send Input";
        
        public override string ActionDescription { get; } = "Send input via keyboard/mouse emulation";
        
        protected override void ExecuteInternal()
        {
            Error = null;

            if (Hotkey == null || Hotkey.IsEmpty)
            {
                return;
            }

            try
            {
                winActivateAction.Execute();
                using (blockUserInput ? userInputBlocker.Block() : Disposable.Empty)
                {
                    PerformInput();
                }
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to send input, hotkey: {hotkey}", e);
                throw;
            }
        }

        private void MoveMouseTo(Point location)
        {
            var screenSize = SystemInformation.VirtualScreen;
            var pLocation = new Point(
                (int)((double)location.X / screenSize.Width * 65535),
                (int)((double)location.Y / screenSize.Height * 65535));
            inputSimulator.Mouse.MoveMouseTo(pLocation.X, pLocation.Y);
            inputSimulator.Mouse.Sleep(KeyStrokeDelay);
        }

        private void PerformInput()
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

            if (Hotkey.MouseButton != null)
            {
                var initialMouseLocation = System.Windows.Forms.Cursor.Position;
                if (MouseLocation.X > 0 || MouseLocation.Y > 0)
                {
                    var pLocation = MouseLocation;
                    var activeWindow = WindowSelector.ActiveWindow;
                    if (activeWindow != null && activeWindow.WindowBounds.IsNotEmpty())
                    {
                        Log.Debug($"Using location of window {activeWindow.Title} ({activeWindow.WindowBounds}), relative coordinates: {pLocation}");
                        pLocation = new Point(pLocation.X + WindowSelector.ActiveWindow.WindowBounds.Left, pLocation.Y + WindowSelector.ActiveWindow.WindowBounds.Top);
                    }
                    Log.Debug($"Moving mouse to {pLocation}, initial mouse coordinates: {initialMouseLocation}");
                    MoveMouseTo(pLocation);
                }
                switch (Hotkey.MouseButton)
                {
                    case MouseButton.Left:
                        inputSimulator.Mouse.LeftButtonDown();
                        inputSimulator.Mouse.Sleep(KeyStrokeDelay);
                        inputSimulator.Mouse.LeftButtonUp();
                        break;
                    case MouseButton.Middle:
                        inputSimulator.Mouse.MiddleButtonDown();
                        inputSimulator.Mouse.Sleep(KeyStrokeDelay);
                        inputSimulator.Mouse.MiddleButtonUp();
                        break;
                    case MouseButton.Right:
                        inputSimulator.Mouse.RightButtonDown();
                        inputSimulator.Mouse.Sleep(KeyStrokeDelay);
                        inputSimulator.Mouse.RightButtonUp();
                        break;
                    case MouseButton.XButton1:
                        inputSimulator.Mouse.XButtonDown(1);
                        inputSimulator.Mouse.Sleep(KeyStrokeDelay);
                        inputSimulator.Mouse.XButtonUp(1);
                        break;
                    case MouseButton.XButton2:
                        inputSimulator.Mouse.XButtonDown(2);
                        inputSimulator.Mouse.Sleep(KeyStrokeDelay);
                        inputSimulator.Mouse.XButtonUp(2);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Mouse button ${Hotkey.MouseButton} is not supported");
                }

                if (RestoreMousePosition)
                {
                    Log.Debug($"Restoring mouse coordinates to {initialMouseLocation}");
                    inputSimulator.Mouse.Sleep(KeyStrokeDelay);
                    MoveMouseTo(initialMouseLocation);
                }
            } else if (Hotkey.Key != Key.None)
            {
                inputSimulator.Keyboard.KeyDown(vk);
                inputSimulator.Keyboard.Sleep(KeyStrokeDelay);
                inputSimulator.Keyboard.KeyUp(vk);
            }

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
    }
}