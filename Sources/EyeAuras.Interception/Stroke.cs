using System.Runtime.InteropServices;

namespace EyeAuras.Interception
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct Stroke
    {
        [FieldOffset(0)] public MouseStroke Mouse;

        [FieldOffset(0)] public KeyStroke Key;
    }
}