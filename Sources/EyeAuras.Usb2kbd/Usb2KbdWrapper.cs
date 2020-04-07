using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;
using EyeAuras.Interception;
using log4net;
using PInvoke;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace EyeAuras.Usb2kbd
{
    internal sealed class Usb2KbdWrapper : DisposableReactiveObject, IKeyboardSimulator
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Usb2KbdWrapper));
        private static readonly string Usb2KbdDllName = $"usb2kbd.dll";
        private static readonly int MinDelayInMilliseconds = 10;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int InvokeMethod(Usb2KbdEventType eventType, int keyCode, int eventValue, int mouseCoords, string deviceId, int b6);

        private readonly IConverter<VirtualKeyCode, UsbHidScanCodes> keysConverter = new KeyToUsbHidScanCodeConverter();
        private readonly Kernel32.SafeLibraryHandle usb2KbdDllHandle;

        private Usb2KbdConfig config;
        private InvokeMethod dllMethod;
        
        public Usb2KbdWrapper()
        {
            Log.Info($"Initializing Usb2Kbd native DLL {Usb2KbdDllName}");
            
            var dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Environment.Is64BitProcess
                ? "x64"
                : "x86", Usb2KbdDllName);

            Log.Debug($"Loading Usb2kbd DLL from {dllPath}");
            usb2KbdDllHandle = Kernel32.LoadLibrary(dllPath);
            if (usb2KbdDllHandle.IsInvalid)
            {
                throw new ApplicationException($"Failed to load Usb2Kbd DLL from {dllPath} (exists: {File.Exists(dllPath)})");
            }

            usb2KbdDllHandle.AddTo(Anchors);

            this.WhenAnyValue(x => x.Config)
                .Where(x => x != null)
                .Subscribe(ApplyConfig, Log.HandleUiException)
                .AddTo(Anchors);
        }

        private void ApplyConfig(Usb2KbdConfig cfg)
        {
            if (string.IsNullOrEmpty(config.MethodName))
            {
                throw new InvalidOperationException($"Usb2Kbd method name is not set, current config: {(config == null ? "not loaded yet" : config?.DumpToTextRaw())}");
            }
            
            Log.Debug($"Loading Usb2Kbd method address using name {cfg.MethodName}");
            var pAddressOfFunctionToCall = Kernel32.GetProcAddress(usb2KbdDllHandle, cfg.MethodName);
            if (pAddressOfFunctionToCall == IntPtr.Zero)
            {
                throw new ApplicationException($"Failed to find method {cfg.MethodName} in Usb2Kbd DLL {Usb2KbdDllName}");
            }

            dllMethod = (InvokeMethod)Marshal.GetDelegateForFunctionPointer(
                pAddressOfFunctionToCall,
                typeof(InvokeMethod));
            
            Log.Debug($"Resetting Usb2Kbd using config {cfg.DumpToTextRaw()}");
            PerformCall(Usb2KbdEventType.AllKeysUp, 0, 0, 0);
        }

        public Usb2KbdConfig Config
        {
            get => config;
            set => this.RaiseAndSetIfChanged(ref config, value);
        }

        private int PerformCall(Usb2KbdEventType eventType, int keyCode, int eventValue, int mouseCoords)
        {
            if (dllMethod == null)
            {
                throw new InvalidOperationException($"Method is not loaded yet, current config: {(config == null ? "not loaded yet" : config?.DumpToTextRaw())}");
            }

            if (string.IsNullOrEmpty(config.DeviceId))
            {
                throw new InvalidOperationException($"Usb2Kbd device id is not set, current config: {(config == null ? "not loaded yet" : config?.DumpToTextRaw())}");
            }
            if (config.SerialNumber == 0)
            {
                throw new InvalidOperationException($"Usb2Kbd serial number is not set, current config: {(config == null ? "not loaded yet" : config?.DumpToTextRaw())}");
            }
            
            if (eventType == Usb2KbdEventType.KeyDown || 
                eventType == Usb2KbdEventType.KeyUp)
            {
                Guard.ArgumentIsBetween(() => keyCode, 4, 231, true);
            }
            if (eventType == Usb2KbdEventType.MouseAbsolute || 
                eventType == Usb2KbdEventType.MouseRelative || 
                eventType == Usb2KbdEventType.MouseAbsoluteNoAcceleration || 
                eventType == Usb2KbdEventType.MouseRelativeNoAcceleration)
            {
                Guard.ArgumentIsBetween(() => keyCode, 0, 7, true);
            }
            
            var resultCode = dllMethod(eventType, keyCode, eventValue, mouseCoords, config.DeviceId, config.SerialNumber);
            if (resultCode != 1)
            {
                if (eventType == Usb2KbdEventType.KeyDown || eventType == Usb2KbdEventType.KeyUp)
                {
                    Log.Warn($"Failed to perform keyboard operation, trying to restore keyboard state, eventType={eventType}({(int)eventType}), keyCode: {keyCode}, eventValue: {eventValue}, mouseCoords: {mouseCoords}");
                    if (PerformCall(Usb2KbdEventType.AllKeysUp, 0, 0, 0) != 1)
                    {
                        Log.Warn($"Failed to restore keyboard state");
                    }
                }
            }

            return resultCode;
        }

        private IKeyboardSimulator PerformCallOrThrow(Usb2KbdEventType eventType, int keyCode, int eventValue, int mouseCoords)
        {
            var resultCode = PerformCall(eventType, keyCode, eventValue, mouseCoords);
            if (resultCode != 1)
            {
                throw new ApplicationException($"Failed to perform call, eventType={eventType}({(int)eventType}), keyCode: {keyCode}, eventValue: {eventValue}, mouseCoords: {mouseCoords}");
            }

            return this;
        }
        
        public IKeyboardSimulator KeyDown(VirtualKeyCode keyCode)
        {
           var scanCode = keysConverter.Convert(keyCode);
           if (scanCode == UsbHidScanCodes.KEY_NONE)
           {
               throw new ApplicationException($"Failed to convert {nameof(VirtualKeyCode)} {keyCode} to {nameof(UsbHidScanCodes)}");
           }
           return PerformCallOrThrow(Usb2KbdEventType.KeyDown, (int)scanCode, 0 ,0);
        }
        
        public IKeyboardSimulator KeyPress(VirtualKeyCode keyCode)
        {
            throw new NotImplementedException();
        }

        public IKeyboardSimulator KeyPress(params VirtualKeyCode[] keyCodes)
        {
            throw new NotImplementedException();
        }

        public IKeyboardSimulator KeyUp(VirtualKeyCode keyCode)
        {
            var scanCode = keysConverter.Convert(keyCode);
            if (scanCode == UsbHidScanCodes.KEY_NONE)
            {
                throw new ApplicationException($"Failed to convert {nameof(VirtualKeyCode)} {keyCode} to {nameof(UsbHidScanCodes)}");
            }
            return PerformCallOrThrow(Usb2KbdEventType.KeyUp, (int)scanCode, 0 ,0);
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

        public IKeyboardSimulator Sleep(int millisecondsTimeout)
        {
            if (MinDelayInMilliseconds > millisecondsTimeout)
            {
                millisecondsTimeout = MinDelayInMilliseconds;
            }
            Thread.Sleep(millisecondsTimeout);
            return this;
        }

        public IKeyboardSimulator Sleep(TimeSpan timeout)
        {
            return Sleep((int)timeout.TotalMilliseconds);
        }

        public IMouseSimulator Mouse { get; }
    }
}