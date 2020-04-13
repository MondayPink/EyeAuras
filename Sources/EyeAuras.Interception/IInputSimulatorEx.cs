using System;
using WindowsInput;

namespace EyeAuras.Interception
{
    public interface IInputSimulatorEx : IInputSimulator
    {
        bool IsAvailable { get; }
    }
}