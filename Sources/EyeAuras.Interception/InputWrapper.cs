using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using log4net;
using PInvoke;

namespace EyeAuras.Interception
{
    internal sealed class InputWrapper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(InputWrapper));
        private Thread callbackThread;
        private IntPtr context;
        private int keyboardDeviceId; 
        private int mouseDeviceId; 
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
            MouseFilterMode = MouseFilterMode.None;
        }

        /// <summary>
        ///     Determines whether the driver traps no keyboard events, all events, or a range of events in-between (down only, up
        ///     only...etc). Set this before loading otherwise the driver will not filter any events and no keypresses can be sent.
        /// </summary>
        public KeyboardFilterMode KeyboardFilterMode { get; set; }
        
        public MouseFilterMode MouseFilterMode { get; set; }

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

            callbackThread = new Thread(KeyboardCallback)
            {
                Priority = ThreadPriority.Highest, 
                IsBackground = true,
                Name = "InterceptionKeyboard"
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

            keyboardDeviceId = 0;
            callbackThread.Abort();
            InterceptionDriver.DestroyContext(context);
            IsLoaded = false;
        }
        
        private void KeyboardCallback()
        {
            try
            {
                InterceptionDriver.SetFilter(context, InterceptionDriver.IsKeyboard, (int) KeyboardFilterMode);
                InterceptionDriver.SetFilter(context, InterceptionDriver.IsMouse, (int)MouseFilterMode.All);

                var stroke = new Stroke();
                int deviceId;
                while (InterceptionDriver.Receive(context, deviceId = InterceptionDriver.Wait(context), ref stroke, 1) > 0)
                {
                    if (keyboardDeviceId <= 0 && InterceptionDriver.IsKeyboard(deviceId) != 0)
                    {
                        keyboardDeviceId = deviceId;
                        Log.Info($"Keyboard detected, deviceId: {deviceId}");
                    }
                    
                    if (mouseDeviceId <= 0 && InterceptionDriver.IsMouse(deviceId) != 0)
                    {
                        mouseDeviceId = deviceId;
                        Log.Info($"Mouse detected, deviceId: {deviceId}");
                    }
                    
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
            }
        }

        public void SendKey(uint interceptionKey, KeyState state)
        {
            EnsureKeyboardDetected();
            var stroke = new Stroke();
            var keyStroke = new KeyStroke
            {
                Code = interceptionKey, State = state
            };
            stroke.Key = keyStroke;
            Log.Debug($"Sending keystroke to KeyboardDeviceId {keyboardDeviceId}: {keyStroke}");
            InterceptionDriver.Send(context, keyboardDeviceId, ref stroke, 1);
        }
        
        public void SendMouseEvent(MouseState state)
        {
            EnsureMouseDetected();

            var stroke = new Stroke();
            var mouseStroke = new MouseStroke {State = state};

            if (state == MouseState.ScrollUp)
            {
                mouseStroke.Rolling = 120;
            }
            else if (state == MouseState.ScrollDown)
            {
                mouseStroke.Rolling = -120;
            }
            Log.Debug($"Sending mouse event to mouseDeviceId{mouseDeviceId}: {mouseStroke}");

            stroke.Mouse = mouseStroke;
            InterceptionDriver.Send(context, mouseDeviceId, ref stroke, 1);
        }

        public void ScrollMouse(ScrollDirection direction)
        {
            EnsureMouseDetected();
            Log.Debug($"Scrolling mouse wheel, direction: {direction}");

            switch (direction)
            { 
                case ScrollDirection.Down:
                    SendMouseEvent(MouseState.ScrollDown);
                    break;
                case ScrollDirection.Up:
                    SendMouseEvent(MouseState.ScrollUp);
                    break;
            }
        }

        /// <summary>
        /// Warning: This function, if using the driver, does not function reliably and often moves the mouse in unpredictable vectors. An alternate version uses the standard Win32 API to get the current cursor's position, calculates the desired destination's offset, and uses the Win32 API to set the cursor to the new position.
        /// </summary>
        public void MoveMouseBy(int deltaX, int deltaY, bool useDriver = false)
        {
            Log.Debug($"Moving mouse by DeltaX:{deltaX} DeltaY:{deltaX} (useDriver: {useDriver})");
            if (useDriver)
            {
                EnsureMouseDetected();
                Stroke stroke = new Stroke();
                MouseStroke mouseStroke = new MouseStroke();

                mouseStroke.X = deltaX;
                mouseStroke.Y = deltaY;

                stroke.Mouse = mouseStroke;
                stroke.Mouse.Flags = MouseFlags.MoveRelative;

                InterceptionDriver.Send(context, mouseDeviceId, ref stroke, 1);
            }
            else
            {
                var currentPos = Cursor.Position;
                Cursor.Position = new Point(currentPos.X + deltaX, currentPos.Y - deltaY); // Coordinate system for y: 0 begins at top, and bottom of screen has the largest number
            }
        }

        /// <summary>
        /// Warning: This function, if using the driver, does not function reliably and often moves the mouse in unpredictable vectors. An alternate version uses the standard Win32 API to set the cursor's position and does not use the driver.
        /// </summary>
        public void MoveMouseTo(int x, int y, bool useDriver = false)
        {
            Log.Debug($"Moving mouse to X:{x} Y:{y} (useDriver: {useDriver})");
            if (useDriver)
            {
                EnsureMouseDetected();
                Stroke stroke = new Stroke();
                MouseStroke mouseStroke = new MouseStroke();

                mouseStroke.X = x;
                mouseStroke.Y = y;

                stroke.Mouse = mouseStroke;
                stroke.Mouse.Flags = MouseFlags.MoveAbsolute;

                InterceptionDriver.Send(context, mouseDeviceId, ref stroke, 1);
            }
            {
                Cursor.Position = new Point(x, y);
            }
        }
        
        private void EnsureMouseDetected()
        {
            if (mouseDeviceId <= 0)
            {
                throw new ApplicationException($"Mouse is not detected yet, please move mouse pointer or press any button before sending mouse commands");
            }
        }

        private void EnsureKeyboardDetected()
        {
            if (keyboardDeviceId <= 0)
            {
                throw new ApplicationException($"Keyboard is not detected yet, please press any key on the keyboard before sending keystrokes");
            }
        }
    }
}