using System;
using System.Diagnostics;
using System.IO;
using log4net;
using PoeShared;

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
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = true,
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
}