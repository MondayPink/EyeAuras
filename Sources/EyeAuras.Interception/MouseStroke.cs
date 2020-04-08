using System;
using System.Runtime.InteropServices;

namespace EyeAuras.Interception
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MouseStroke
    {
        public MouseState State;
        public MouseFlags Flags;
        public Int16 Rolling;
        public Int32 X;
        public Int32 Y;
        public UInt16 Information;

        public override string ToString()
        {
            return $"{nameof(State)}: {State}, {nameof(Flags)}: {Flags}, {nameof(Rolling)}: {Rolling}, {nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Information)}: {Information}";
        }
    }
}