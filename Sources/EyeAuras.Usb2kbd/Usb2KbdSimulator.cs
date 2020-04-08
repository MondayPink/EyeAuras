using System;
using WindowsInput;
using EyeAuras.Interception;
using log4net;
using PoeShared.Modularity;
using PoeShared.Scaffolding;

namespace EyeAuras.Usb2kbd
{
    internal sealed class Usb2KbdSimulator : DisposableReactiveObject, IInputSimulatorEx
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Usb2KbdSimulator));

        private readonly Usb2KbdWrapper wrapper;
        private bool isAvailable = false;

        public Usb2KbdSimulator(IConfigProvider<Usb2KbdConfig> configProvider)
        {
            try
            {
                wrapper = new Usb2KbdWrapper();
                
                var defaultSimulator = new InputSimulator();
                Mouse = defaultSimulator.Mouse;
                InputDeviceState = defaultSimulator.InputDeviceState;
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to initialize Usb2Kbd", ex);
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

        public IKeyboardSimulator Keyboard => wrapper;

        public IMouseSimulator Mouse { get; }
        
        public IInputDeviceStateAdaptor InputDeviceState { get; }

        public bool IsAvailable
        {
            get => isAvailable;
            set => this.RaiseAndSetIfChanged(ref isAvailable, value);
        }
    }
}