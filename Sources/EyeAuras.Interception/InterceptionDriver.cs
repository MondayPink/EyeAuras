using System;
using System.Runtime.InteropServices;

namespace EyeAuras.Interception
{
    /// <summary>
    ///     The .NET wrapper class around the C++ library interception.dll.
    /// </summary>
    internal static class InterceptionDriver
    {
        public const string InterceptionLibraryName = "interception.dll";
        
        [DllImport(InterceptionLibraryName, EntryPoint = "interception_create_context", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateContext();

        [DllImport(InterceptionLibraryName, EntryPoint = "interception_destroy_context", CallingConvention = CallingConvention.Cdecl)]
        public static extern void DestroyContext(IntPtr context);

        [DllImport(InterceptionLibraryName, EntryPoint = "interception_get_precedence", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetPrecedence(IntPtr context, int device);

        [DllImport(InterceptionLibraryName, EntryPoint = "interception_set_precedence", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetPrecedence(IntPtr context, int device, int precedence);

        [DllImport(InterceptionLibraryName, EntryPoint = "interception_get_filter", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetFilter(IntPtr context, int device);

        [DllImport(InterceptionLibraryName, EntryPoint = "interception_set_filter", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetFilter(IntPtr context, Predicate predicate, int keyboardFilterMode);

        [DllImport(InterceptionLibraryName, EntryPoint = "interception_wait", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Wait(IntPtr context);

        [DllImport(InterceptionLibraryName, EntryPoint = "interception_wait_with_timeout", CallingConvention = CallingConvention.Cdecl)]
        public static extern int WaitWithTimeout(IntPtr context, ulong milliseconds);

        [DllImport(InterceptionLibraryName, EntryPoint = "interception_send", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Send(IntPtr context, int device, ref Stroke stroke, uint numStrokes);

        [DllImport(InterceptionLibraryName, EntryPoint = "interception_receive", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Receive(IntPtr context, int device, ref Stroke stroke, uint numStrokes);

        [DllImport(InterceptionLibraryName, EntryPoint = "interception_get_hardware_id", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetHardwareId(IntPtr context, int device, string hardwareIdentifier, uint sizeOfString);

        [DllImport(InterceptionLibraryName, EntryPoint = "interception_is_invalid", CallingConvention = CallingConvention.Cdecl)]
        public static extern int IsInvalid(int device);

        [DllImport(InterceptionLibraryName, EntryPoint = "interception_is_keyboard", CallingConvention = CallingConvention.Cdecl)]
        public static extern int IsKeyboard(int device);

        [DllImport(InterceptionLibraryName, EntryPoint = "interception_is_mouse", CallingConvention = CallingConvention.Cdecl)]
        public static extern int IsMouse(int device);
    }
}