using WindowsInput;
using EyeAuras.Interception;

namespace EyeAuras.Usb2kbd
{
    internal sealed class Usb2KbdInputSimulator : IInputSimulatorEx
    {
        public IKeyboardSimulator Keyboard { get; }
        
        public IMouseSimulator Mouse { get; }
        
        public IInputDeviceStateAdaptor InputDeviceState { get; }
        
        public bool IsAvailable { get; }
    }
}