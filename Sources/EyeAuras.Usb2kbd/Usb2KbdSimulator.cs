using System;
using System.Reactive.Disposables;
using System.Windows.Input;
using WindowsInput;
using EyeAuras.Interception;
using log4net;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity;

namespace EyeAuras.Usb2kbd
{
    internal sealed class Usb2KbdSimulator : DisposableReactiveObject, IInputSimulatorEx
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Usb2KbdSimulator));

        private readonly Usb2KbdWrapper wrapper;
        private bool isAvailable = false;

        public Usb2KbdSimulator(
            [Dependency(WellKnownKeyboardSimulators.InputSimulator)] IInputSimulatorEx defaultSimulator,
            IFactory<Usb2KbdWrapper> wrapperFactory,
            IConfigProvider<Usb2KbdConfig> configProvider)
        {
            try
            {
                Log.Info($"Initializing Usb2Kbd device");
                InputDeviceState = defaultSimulator.InputDeviceState;
                wrapper = wrapperFactory.Create();
                Keyboard = wrapper;
                Mouse = wrapper;
                Log.Info($"Successfully initialized Usb2Kbd device");
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to initialize Usb2Kbd device", ex);
                Keyboard = defaultSimulator.Keyboard;
                Mouse = defaultSimulator.Mouse;
            }

            configProvider.WhenChanged
                .Subscribe(ApplyConfig)
                .AddTo(Anchors);
        }

        private void ApplyConfig(Usb2KbdConfig config)
        {
            try
            {
                Log.Debug($"Applying new Usb2Kbd config: {config.DumpToTextRaw()}");
                wrapper.Config = config;
                IsAvailable = true;
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to apply new Usb2Kbd config: {config.DumpToTextRaw()} - {ex.Message}");
                IsAvailable = false;
            }
        }

        public IKeyboardSimulator Keyboard {get; }

        public IMouseSimulator Mouse { get; }
        
        public IInputDeviceStateAdaptor InputDeviceState { get; }

        public bool IsAvailable
        {
            get => isAvailable;
            set => this.RaiseAndSetIfChanged(ref isAvailable, value);
        }
    }
}