using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;
using PoeShared.Prism;

namespace EyeAuras.Interception
{
    internal sealed class DriverBasedKeyboardSimulator : IKeyboardSimulator
    {
        private readonly InputWrapper wrapper;
        private readonly IConverter<VirtualKeyCode, uint> keysConverter = new KeysConverter();
        private readonly ConcurrentDictionary<VirtualKeyCode, uint> keyCodeToScanCodeMap = new ConcurrentDictionary<VirtualKeyCode, uint>();
        
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
        }

        private uint GetScanCode(VirtualKeyCode keyCode)
        {
            return  keyCodeToScanCodeMap.GetOrAdd(keyCode, x => keysConverter.Convert(x));
        }
        
        public IKeyboardSimulator KeyDown(VirtualKeyCode keyCode)
        {
            var keyToSend = GetScanCode(keyCode);
            wrapper.SendKey(keyToSend, KeyState.Down);
            return this;
        }

        public IKeyboardSimulator KeyPress(VirtualKeyCode keyCode)
        {
            var keyToSend = GetScanCode(keyCode);
            wrapper.SendKey(keyToSend, KeyState.Down);
            Thread.Sleep(new Random().Next(50, 150));
            wrapper.SendKey(keyToSend, KeyState.Up);
            return this;
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
            var keyToSend = GetScanCode(keyCode);
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