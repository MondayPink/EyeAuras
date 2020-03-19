using System.Runtime.InteropServices;

namespace EyeAuras.Interception
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int Predicate(int device);
}