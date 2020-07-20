using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using EyeAuras.UI.Prism;
using log4net;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Squirrel.Prism;
using PoeShared.Wpf.UI.ExceptionViewer;
using Prism.Unity;
using ReactiveUI;
using Unity;

namespace EyeAuras.UI
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly CompositeDisposable anchors = new CompositeDisposable();
        private readonly EyeAurasBootstrapper aurasBootstrapper;

        public App()
        {
            try
            {
                var arguments = Environment.GetCommandLineArgs();
                AppArguments.Instance.AppName = "EyeAuras";

                if (!AppArguments.Parse(arguments))
                {
                    SharedLog.Instance.InitializeLogging("Startup", AppArguments.Instance.AppName);
                    throw new ApplicationException($"Failed to parse command line args: {string.Join(" ", arguments)}");
                }

                InitializeLogging();

                Log.Debug($"Arguments: {arguments.DumpToText()}");
                Log.Debug($"Parsed args: {AppArguments.Instance.DumpToText()}");
                Log.Debug($"OS: { new { Environment.OSVersion, Environment.Is64BitProcess, Environment.Is64BitOperatingSystem }})");
                Log.Debug($"Environment: {new { Environment.MachineName, Environment.UserName, Environment.WorkingSet, Environment.SystemDirectory, Environment.UserInteractive }})");
                Log.Debug($"Runtime: {new { System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, System.Runtime.InteropServices.RuntimeInformation.OSDescription }}");
                Log.Debug($"Culture: {Thread.CurrentThread.CurrentCulture}, UICulture: {Thread.CurrentThread.CurrentUICulture}");
                Log.Debug($"Is Elevated: {AppArguments.Instance.IsElevated}");
                
                var container = new UnityContainer();
                container.AddNewExtension<Diagnostic>();
                container.AddNewExtension<UiRegistrations>();
                container.AddNewExtension<WpfCommonRegistrations>();
                container.AddNewExtension<UpdaterRegistrations>();
                container.AddNewExtension<NativeRegistrations>();
                container.AddNewExtension<CommonRegistrations>();
                aurasBootstrapper = new EyeAurasBootstrapper(container);

                Log.Debug($"UI Scheduler: {RxApp.MainThreadScheduler}");
                RxApp.MainThreadScheduler = DispatcherScheduler.Current;
                RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;
                Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                Log.Debug($"New UI Scheduler: {RxApp.MainThreadScheduler}");
                Log.Debug($"BG Scheduler: {RxApp.TaskpoolScheduler}");
                
                Log.Debug($"Configuring AllowSetForegroundWindow permissions");
                UnsafeNative.AllowSetForegroundWindow();

                Disposable.Create(
                        () =>
                        {
                            Log.Info("Disposing bootstrapper...");
                            aurasBootstrapper.Dispose();
                        })
                    .AddTo(anchors);
            }
            catch (Exception ex)
            {
                ReportCrash(ex);
                throw;
            }
        }

        private static ILog Log => SharedLog.Instance.Log;

        private void SingleInstanceValidationRoutine()
        {
            var mutexId = $"{AppArguments.Instance.AppName}{(AppArguments.Instance.IsDebugMode ? "DEBUG" : "RELEASE")}{{B74259C4-0F20-4EC2-9538-BA8A176FDF7D}}";
            Log.Debug($"Acquiring mutex {mutexId}...");
            var mutex = new Mutex(true, mutexId);
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                Log.Debug($"Mutex {mutexId} was successfully acquired");

                AppDomain.CurrentDomain.DomainUnload += delegate
                {
                    Log.Debug($"[App.DomainUnload] Detected DomainUnload, disposing mutex {mutexId}");
                    mutex.ReleaseMutex();
                    Log.Debug("[App.DomainUnload] Mutex was successfully disposed");
                };
            }
            else
            {
                Log.Warn($"Application is already running, mutex: {mutexId}");
                ShowShutdownWarning();
            }
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ReportCrash(e.ExceptionObject as Exception, "CurrentDomainUnhandledException");
        }

        private void DispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ReportCrash(e.Exception, "DispatcherUnhandledException");
        }

        private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            ReportCrash(e.Exception, "TaskSchedulerUnobservedTaskException");
        }

        private void InitializeLogging()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Dispatcher.CurrentDispatcher.UnhandledException += DispatcherOnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
            RxApp.DefaultExceptionHandler = SharedLog.Instance.Errors;
            if (AppArguments.Instance.IsDebugMode)
            {
                SharedLog.Instance.InitializeLogging("Debug", AppArguments.Instance.AppName);
            }
            else
            {
                SharedLog.Instance.InitializeLogging("Release", AppArguments.Instance.AppName);
            }

            var logFileConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");
            SharedLog.Instance.LoadLogConfiguration(new FileInfo(logFileConfigPath));
            SharedLog.Instance.AddTraceAppender().AddTo(anchors);
            SharedLog.Instance.Errors.Subscribe(
                ex =>
                {
                    ReportCrash(ex);
                }).AddTo(anchors);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Log.Debug("Application startup detected");

            SingleInstanceValidationRoutine();

            Log.Info("Starting bootstrapper...");
            aurasBootstrapper.Run();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            Log.Debug("Application exit detected");
            anchors.Dispose();
        }

        private void ShowShutdownWarning()
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName();
            var window = MainWindow;
            var title = $"{assemblyName.Name} v{assemblyName.Version}";
            var message = "Application is already running !";
            if (window != null)
            {
                MessageBox.Show(window, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            Log.Warn("Shutting down...");
            Environment.Exit(0);
        }
        
        private void ReportCrash(Exception exception, string developerMessage = "")
        {
            Log.Error($"Unhandled application exception({developerMessage})", exception);

            AppDomain.CurrentDomain.UnhandledException -= CurrentDomainOnUnhandledException;
            TaskScheduler.UnobservedTaskException -= TaskSchedulerOnUnobservedTaskException;
            Dispatcher.CurrentDispatcher.UnhandledException -= DispatcherOnUnhandledException;
            
            var reporter = aurasBootstrapper.Container.Resolve<IExceptionDialogDisplayer>();
            reporter.ShowDialogAndTerminate(exception);
        }
    }
}