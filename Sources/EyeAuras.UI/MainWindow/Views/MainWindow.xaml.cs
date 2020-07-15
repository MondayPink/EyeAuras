using System;
using System.Windows;
using MahApps.Metro.Controls;

namespace EyeAuras.UI.MainWindow.Views
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            Application.Current.Exit += ApplicationOnExit;
        }

        private void ApplicationOnExit(object sender, ExitEventArgs exitEventArgs)
        {
            (DataContext as IDisposable)?.Dispose();
        }
    }
}