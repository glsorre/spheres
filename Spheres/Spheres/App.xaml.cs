using System;
using System.Linq;
using FlaUI.Core.WindowsAPI;
using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Spheres.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Spheres
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public TaskbarIcon? TrayIcon { get; private set; }
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();

            TrayInit();
        }

        private void TrayInit()
        {
            var showHideWindowCommand = (XamlUICommand)Resources["ShowHideWindowCommand"];
            showHideWindowCommand.ExecuteRequested += ShowHideWindowCommand_ExecuteRequested;

            var exitApplicationCommand = (XamlUICommand)Resources["ExitApplicationCommand"];
            exitApplicationCommand.ExecuteRequested += ExitApplicationCommand_ExecuteRequested;

            var launchCloseSphereCommand = (XamlUICommand)Resources["LaunchCloseSphereCommand"];
            launchCloseSphereCommand.ExecuteRequested += LaunchCloseSphereCommand_ExecuteRequested;

            TrayIcon = (TaskbarIcon)Resources["TrayIcon"];
            TrayIcon.ForceCreate();
        }

        private void LaunchCloseSphereCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            m_window.DispatcherQueue.TryEnqueue(async () =>
            {
                var sphereName = args.Parameter.ToString();
                if (sphereName != null)
                {
                    var sphere = m_window.AppViewModel.Spheres.FirstOrDefault(s => s.Name == sphereName);
                    if (sphere != null)
                    {
                        await sphere.Toggle();
                    }
                }
            });
        }

        private void ShowHideWindowCommand_ExecuteRequested(object? _, ExecuteRequestedEventArgs args)
        {
            if (m_window.Visible)
            {
                m_window.Hide();
            }
            else
            {
                m_window.Show();
                var Handle = m_window.GetWindowHandle();
                User32.SetForegroundWindow(Handle);
            }
        }

        private void ExitApplicationCommand_ExecuteRequested(object? _, ExecuteRequestedEventArgs args)
        {
            TrayIcon?.Dispose();
            m_window?.Close();

            // https://github.com/HavenDV/H.NotifyIcon/issues/66
            if (m_window == null)
            {
                Environment.Exit(0);
            }
        }

        public static MainWindow? m_window;
    }
}
