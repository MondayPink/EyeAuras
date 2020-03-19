using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using log4net;
using PInvoke;

namespace EyeAuras.Interception
{
    internal sealed class InputWrapper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(InputWrapper));
        private Thread callbackThread;
        private IntPtr context;
        private int deviceId; /* Very important; which device the driver sends events to */
        private readonly SafeHandle interceptionLibraryHandle;

        public InputWrapper()
        {
            context = IntPtr.Zero;
            Log.Debug($"Initializing Interception driver, is64BitProcess: {Environment.Is64BitProcess}, appDomain: {AppDomain.CurrentDomain.BaseDirectory}");
            if (Environment.Is64BitProcess)
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "x64", InterceptionDriver.InterceptionLibraryName);
                interceptionLibraryHandle = Kernel32.LoadLibrary(path);
            }
            else
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "x86", InterceptionDriver.InterceptionLibraryName);
                interceptionLibraryHandle = Kernel32.LoadLibrary(path);
            }
            if (interceptionLibraryHandle.IsInvalid)
            {
                throw new ApplicationException($"Failed to load {InterceptionDriver.InterceptionLibraryName} - unknown error");
            }
            if (interceptionLibraryHandle.IsInvalid)
            {
                throw new ApplicationException($"Failed to load {InterceptionDriver.InterceptionLibraryName} - handle is invalid");
            }
            if (interceptionLibraryHandle.IsClosed)
            {
                throw new ApplicationException($"Failed to load {InterceptionDriver.InterceptionLibraryName} - handle is closed");
            }

            KeyboardFilterMode = KeyboardFilterMode.None;
        }

        /// <summary>
        ///     Determines whether the driver traps no keyboard events, all events, or a range of events in-between (down only, up
        ///     only...etc). Set this before loading otherwise the driver will not filter any events and no keypresses can be sent.
        /// </summary>
        public KeyboardFilterMode KeyboardFilterMode { get; set; }

        public bool IsLoaded { get; private set; }

        /*
         * Attempts to load the driver. You may get an error if the C++ library 'interception.dll' is not in the same folder as the executable and other DLLs. MouseFilterMode and KeyboardFilterMode must be set before Load() is called. Calling Load() twice has no effect if already loaded.
         */
        public bool Load()
        {
            if (IsLoaded)
            {
                return false;
            }
            
            context = InterceptionDriver.CreateContext();
            if (context == IntPtr.Zero)
            {
                IsLoaded = false;
                return false;
            }

            callbackThread = new Thread(DriverCallback)
            {
                Priority = ThreadPriority.Highest, 
                IsBackground = true,
                Name = "Interception"
            };
            callbackThread.Start();

            IsLoaded = true;
            return true;
        }

        /*
         * Safely unloads the driver. Calling Unload() twice has no effect.
         */
        public void Unload()
        {
            if (!IsLoaded)
            {
                return;
            }

            if (context == IntPtr.Zero)
            {
                return;
            }

            callbackThread.Abort();
            InterceptionDriver.DestroyContext(context);
            IsLoaded = false;
        }

        private void DriverCallback()
        {
            try
            {
                InterceptionDriver.SetFilter(context, InterceptionDriver.IsKeyboard, (int) KeyboardFilterMode);

                var stroke = new Stroke();
                while (InterceptionDriver.Receive(context, deviceId = InterceptionDriver.Wait(context), ref stroke, 1) > 0)
                {
                    InterceptionDriver.Send(context, deviceId, ref stroke, 1);
                }
            }
            catch (Exception e)
            {
                Log.Error("Error in driver thread", e);
            }
            finally
            {
                Log.Debug($"Driver thread terminated, unloading context");
                Unload();
            }
        }

        public void SendKey(uint interceptionKey, KeyState state)
        {
            var stroke = new Stroke();
            var keyStroke = new KeyStroke
            {
                Code = interceptionKey, State = state
            };
            stroke.Key = keyStroke;
            InterceptionDriver.Send(context, deviceId, ref stroke, 1);
        }
    }
}