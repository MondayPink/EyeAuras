﻿using System;
using System.Reactive.Disposables;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using EyeAuras.UI.SplashWindow.ViewModels;
using log4net;
using PoeShared;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.SplashWindow.Services
{
    internal sealed class SplashWindowDisplayer : DisposableReactiveObject
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SplashWindowDisplayer));

        private readonly Window mainWindow;
        private Window splashWindow;

        public SplashWindowDisplayer(Window mainWindow)
        {
            Guard.ArgumentNotNull(mainWindow, nameof(mainWindow));

            this.mainWindow = mainWindow;
            Log.Debug($"Splash displayer created, main window state: {FormatMainWindowState()}");

            var thread = new Thread(
                () =>
                {
                    try
                    {
                        Log.Debug("Splash window thread started");

                        var dispatcher = Dispatcher.CurrentDispatcher;

                        Disposable.Create(
                                () =>
                                {
                                    Log.Debug("Shutting down Dispatcher");
                                    dispatcher.InvokeShutdown();
                                    Log.Debug("Dispatcher has been shut down");
                                })
                            .AddTo(Anchors);

                        dispatcher.BeginInvoke(
                            (Action) (() =>
                            {
                                splashWindow = new Window
                                {
                                    Content = new SplashWindowViewModel(),
                                    Background = null,
                                    AllowsTransparency = true,
                                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                                    ShowActivated = true,
                                    ShowInTaskbar = true,
                                    ResizeMode = ResizeMode.NoResize,
                                    SizeToContent = SizeToContent.WidthAndHeight
                                };
                                splashWindow.WindowStyle = WindowStyle.None;

                                Log.Debug("Showing splash window...");
                                splashWindow.ShowDialog();
                                Log.Debug("Splash window closed");
                            }));
                        Dispatcher.Run();
                    }
                    catch (Exception e)
                    {
                        Log.Error("Error", e);
                    }
                    finally
                    {
                        Log.Debug("Splash window thread terminated");
                    }
                });
            thread.Name = "SplashWindow";
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }

        public void Close()
        {
            Log.Debug($"Closing splash and opening Main window, state: {FormatMainWindowState()}");
            CloseWindowSafe(splashWindow);

            mainWindow.Opacity = 1;
            mainWindow.ShowInTaskbar = true;

            Dispose();
        }

        public void Show()
        {
            Log.Debug($"Initializing Main window, state: {FormatMainWindowState()}");

            mainWindow.ShowInTaskbar = false;
            mainWindow.ShowActivated = true;
            mainWindow.WindowStyle = WindowStyle.None;
            mainWindow.AllowsTransparency = true;
            mainWindow.Opacity = 0;
            mainWindow.Show();
            mainWindow.Activate();
            Log.Debug($"Initiated Main window loading, state: {FormatMainWindowState()}");
        }

        private static void CloseWindowSafe(Window window)
        {
            if (window == null)
            {
                return;
            }

            if (window.Dispatcher.CheckAccess())
            {
                window.Close();
            }
            else
            {
                window.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(window.Close));
            }
        }

        private string FormatMainWindowState()
        {
            return $"{new {mainWindow.Opacity, mainWindow.IsVisible, mainWindow.IsLoaded, mainWindow.Visibility}}";
        }
    }
}