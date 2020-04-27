using System;
using WindowsInput;
using log4net;
using PoeShared.Modularity;

namespace EyeAuras.Interception
{
    internal sealed class DriverBasedInputSimulator : IInputSimulatorEx
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DriverBasedInputSimulator));

        public DriverBasedInputSimulator(IAppArguments appArguments)
        {
            var defaultSimulator = new InputSimulator();
            InputDeviceState = defaultSimulator.InputDeviceState;

            try
            {                
                Log.Info($"Initializing Interception driver-based input simulator");
                var simulator = new DriverBasedKeyboardSimulator(appArguments);
                Keyboard = simulator;
                Mouse = simulator;
                Log.Info($"Successfully loaded Interception driver-based input simulator");

                IsAvailable = true;
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load Interception driver-based keyboard simulator, falling back to {typeof(InputSimulator)}");
                Keyboard = defaultSimulator.Keyboard;
                Mouse = defaultSimulator.Mouse;
                IsAvailable = false;
            }
        }

        public IKeyboardSimulator Keyboard { get; }
        
        public IMouseSimulator Mouse { get; }
        
        public IInputDeviceStateAdaptor InputDeviceState { get; }
        
        public bool IsAvailable { get; }
    }
}