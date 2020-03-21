using System;
using WindowsInput;
using log4net;

namespace EyeAuras.Interception
{
    internal sealed class DriverBasedInputSimulator : IInputSimulatorEx
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DriverBasedInputSimulator));

        public DriverBasedInputSimulator()
        {
            var defaultSimulator = new InputSimulator();
            Mouse = defaultSimulator.Mouse;
            InputDeviceState = defaultSimulator.InputDeviceState;
            
            try
            {
                Keyboard = new DriverBasedKeyboardSimulator();
                Log.Info($"Successfully loaded Interception driver-based input simulator");

                IsAvailable = true;
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load Interception driver-based keyboard simulator, falling back to {typeof(InputSimulator)}");
                Keyboard = defaultSimulator.Keyboard;
                IsAvailable = false;
            }
        }

        public IKeyboardSimulator Keyboard { get; }
        public IMouseSimulator Mouse { get; }
        public IInputDeviceStateAdaptor InputDeviceState { get; }
        public bool IsAvailable { get; }
    }
}