using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using H.NotifyIcon.Core;
using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
            //m_window.Activate();

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
            m_window.DispatcherQueue.TryEnqueue(() =>
            {
                var sphereName = args.Parameter.ToString();
                if (sphereName != null)
                {
                    if (m_window.SavedSpheres.ContainsKey(sphereName))
                    {
                        m_window.SavedSpheres[sphereName].Toggle();
                    }
                }
            });
        }

        public void AddTrayItem(string sphereName)
        {
            if (TrayIcon.ContextFlyout is MenuFlyout menuFlyout)
            {
                var checkedBinding = new Binding()
                {
                    Path = new PropertyPath("SavedSpheres[" + sphereName + "].IsRunning"),
                    Source = m_window,
                    Mode = BindingMode.OneWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                };

                var trayItem = new ToggleMenuFlyoutItem()
                {
                    Text = sphereName,
                    Command = (XamlUICommand)Resources["LaunchCloseSphereCommand"],
                    CommandParameter = sphereName,
                };

                trayItem.SetBinding(ToggleMenuFlyoutItem.IsCheckedProperty, checkedBinding);

                menuFlyout.Items.Insert(0, trayItem);
            }
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
