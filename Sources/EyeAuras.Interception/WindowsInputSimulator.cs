using WindowsInput;

namespace EyeAuras.Interception
{
    internal sealed class WindowsInputSimulator : InputSimulator, IInputSimulatorEx
    {
        public bool IsAvailable { get; } = true;
    }
}