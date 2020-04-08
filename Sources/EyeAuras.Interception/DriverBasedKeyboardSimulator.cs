using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using PoeShared.Prism;

namespace EyeAuras.Interception
{
    internal sealed class DriverBasedKeyboardSimulator : IKeyboardSimulator, IMouseSimulator
    {
        private readonly InputWrapper wrapper;
        private readonly IConverter<VirtualKeyCode, uint> keysConverter = new KeysConverter();
        
        public DriverBasedKeyboardSimulator()
        {
            wrapper = new InputWrapper
            {
                KeyboardFilterMode = KeyboardFilterMode.KeyDown
            };
            if (!wrapper.Load() || !wrapper.IsLoaded)
            {
                throw new ApplicationException($"Failed to load Interception driver");
            }

            Keyboard = this;
            Mouse = this;
        }
        
        public IKeyboardSimulator KeyDown(VirtualKeyCode keyCode)
        {
            var keyToSend = keysConverter.Convert(keyCode);
            wrapper.SendKey(keyToSend, KeyState.Down);
            return this;
        }

        public IKeyboardSimulator KeyPress(VirtualKeyCode keyCode)
        {
            throw new NotImplementedException();
        }

        public IKeyboardSimulator KeyPress(params VirtualKeyCode[] keyCodes)
        {
            foreach (var keyCode in keyCodes)
            {
                KeyPress(keyCode);
            }

            return this;
        }

        public IKeyboardSimulator KeyUp(VirtualKeyCode keyCode)
        {
            var keyToSend = keysConverter.Convert(keyCode);
            wrapper.SendKey(keyToSend, KeyState.Up);
            return this;
        }

        public IKeyboardSimulator ModifiedKeyStroke(IEnumerable<VirtualKeyCode> modifierKeyCodes, IEnumerable<VirtualKeyCode> keyCodes)
        {
            throw new NotImplementedException();
        }

        public IKeyboardSimulator ModifiedKeyStroke(IEnumerable<VirtualKeyCode> modifierKeyCodes, VirtualKeyCode keyCode)
        {
            throw new NotImplementedException();
        }

        public IKeyboardSimulator ModifiedKeyStroke(VirtualKeyCode modifierKey, IEnumerable<VirtualKeyCode> keyCodes)
        {
            throw new NotImplementedException();
        }

        public IKeyboardSimulator ModifiedKeyStroke(VirtualKeyCode modifierKeyCode, VirtualKeyCode keyCode)
        {
            throw new NotImplementedException();
        }

        public IKeyboardSimulator TextEntry(string text)
        {
            throw new NotImplementedException();
        }

        public IKeyboardSimulator TextEntry(char character)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Simulates mouse movement by the specified distance measured as a delta from the current mouse location in pixels.
        /// </summary>
        /// <param name="pixelDeltaX">The distance in pixels to move the mouse horizontally.</param>
        /// <param name="pixelDeltaY">The distance in pixels to move the mouse vertically.</param>
        public IMouseSimulator MoveMouseBy(int pixelDeltaX, int pixelDeltaY)
        {
            wrapper.MoveMouseBy(pixelDeltaX, pixelDeltaY);
            return this;
        }

        /// <summary>
        /// Simulates mouse movement to the specified location on the primary display device.
        /// </summary>
        /// <param name="absoluteX">The destination's absolute X-coordinate on the primary display device where 0 is the extreme left hand side of the display device and 65535 is the extreme right hand side of the display device.</param>
        /// <param name="absoluteY">The destination's absolute Y-coordinate on the primary display device where 0 is the top of the display device and 65535 is the bottom of the display device.</param>
        public IMouseSimulator MoveMouseTo(double absoluteX, double absoluteY)
        {
            var screenSize = SystemInformation.VirtualScreen;
            wrapper.MoveMouseTo((int)(absoluteX / 65535 * screenSize.Width), (int)(absoluteY / 65535 * screenSize.Height));
            return this;
        }

        public IMouseSimulator MoveMouseToPositionOnVirtualDesktop(double absoluteX, double absoluteY)
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator LeftButtonDown()
        {
            wrapper.SendMouseEvent(MouseState.LeftDown);
            return this;
        }

        public IMouseSimulator LeftButtonUp()
        {
            wrapper.SendMouseEvent(MouseState.LeftUp);
            return this;
        }

        public IMouseSimulator LeftButtonClick()
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator LeftButtonDoubleClick()
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator MiddleButtonDown()
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator MiddleButtonUp()
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator MiddleButtonClick()
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator MiddleButtonDoubleClick()
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator RightButtonDown()
        {
            wrapper.SendMouseEvent(MouseState.RightDown);
            return this;
        }

        public IMouseSimulator RightButtonUp()
        {
            wrapper.SendMouseEvent(MouseState.RightUp);
            return this;
        }

        public IMouseSimulator RightButtonClick()
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator RightButtonDoubleClick()
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator XButtonDown(int buttonId)
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator XButtonUp(int buttonId)
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator XButtonClick(int buttonId)
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator XButtonDoubleClick(int buttonId)
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator VerticalScroll(int scrollAmountInClicks)
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator HorizontalScroll(int scrollAmountInClicks)
        {
            throw new NotImplementedException();
        }

        public int MouseWheelClickSize { get; set; }
        
        public IKeyboardSimulator Keyboard { get; }
        
        IMouseSimulator IMouseSimulator.Sleep(int millsecondsTimeout)
        {
            Sleep(millsecondsTimeout);
            return this;
        }

        IMouseSimulator IMouseSimulator.Sleep(TimeSpan timeout)
        {
            Sleep(timeout);
            return this;
        }

        public IKeyboardSimulator Sleep(int millsecondsTimeout)
        {
            Thread.Sleep(millsecondsTimeout);
            return this;
        }

        public IKeyboardSimulator Sleep(TimeSpan timeout)
        {
            Thread.Sleep(timeout);
            return this;
        }

        public IMouseSimulator Mouse { get; }
    }
}