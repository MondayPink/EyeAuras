using System.Runtime.InteropServices;

namespace EyeAuras.Interception
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct KeyStroke
    {
        public uint Code;
        public KeyState State;
        public uint Information;

        public override string ToString()
        {
            return $"{nameof(Code)}: {Code}, {nameof(State)}: {State}, {nameof(Information)}: {Information}";
        }
    }
}