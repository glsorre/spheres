using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Spheres.Models;
using Spheres.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Spheres.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Sphere[] spheres)
            {
                DataContext = new HomeViewModel(spheres);
                if (DataContext is HomeViewModel viewModel)
                {
                    viewModel.isLoading = false;
                }
            }
        }

        private async void LaunchToogle_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                if (toggleSwitch.DataContext is Sphere sphere)
                {
                    if (toggleSwitch.IsOn)
                    {
                        DispatcherQueue.TryEnqueue(async () =>
                        {
                            await sphere.Start();
                        });
                    }
                    else
                    {
                        DispatcherQueue.TryEnqueue(async () =>
                        {
                            await sphere.Stop();
                        });
                    }
                }
            }
        }
    }
}
