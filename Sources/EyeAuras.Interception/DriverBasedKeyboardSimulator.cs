using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;
using log4net;
using PoeShared;
using PoeShared.Prism;

namespace EyeAuras.Interception
{
    public sealed class DriverInstallHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DriverInstallHelper));

        private const string InstallerName = "install-interception.exe";
        
        public void Install()
        {
            RunInstaller("/install");
        }
        
        public void Uninstall()
        {
            RunInstaller("/uninstall");
        }

        private void RunInstaller(string arguments)
        {
            var installerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Interception", InstallerName);
            if (!File.Exists(installerPath))
            {
                throw new FileNotFoundException($"Interception installer not found, path: ${installerPath}");
            }

            Log.Info($"Starting Interception driver installer {installerPath}");
            var installer = new Process
            {
                StartInfo =
                {
                    FileName = installerPath, Arguments = arguments, UseShellExecute = false, 
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            if (!AppArguments.Instance.IsElevated)
            {
                throw new ApplicationException($"Application must be run with Administrator permissions to perform installation");
            }
            installer.Start();  
            installer.WaitForExit();
            Log.Info($"Installer terminated, exit code: {installer.ExitCode}, exitTime: {installer.ExitTime}");

            var installerOutputLog = installer.StandardOutput.ReadToEnd();
            var installerErrorLog = installer.StandardError.ReadToEnd();
            Log.Info($"Installer output log:\n{installerOutputLog}");
            if (!string.IsNullOrEmpty(installerErrorLog))
            {
                Log.Warn($"Installer error log:\n{installerErrorLog}");
            }

            if (!string.IsNullOrEmpty(installerErrorLog))
            {
                throw new ApplicationException($"Installer completed with errors(exitCode: {installer.ExitCode}): {installerErrorLog}");
            }

            if (installer.ExitCode != 0)
            {
                throw new ApplicationException($"Installer failed with exit code {installer.ExitCode}, output: {installerOutputLog}");
            }
        }
    }
    
    public sealed class DriverBasedKeyboardSimulator : IKeyboardSimulator
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