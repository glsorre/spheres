using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
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
using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Spheres.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SpherePage : Page
    {
        public SpherePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var appViewModel = (AppViewModel)e.Parameter;

            DataContext = new SphereViewModel(appViewModel);

            if (DataContext is SphereViewModel viewModel)
            {
                viewModel.IsLoading = false;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SphereViewModel viewModel)
            {
                await viewModel.Save();
            }
        }

        private async void RemoveFacetButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SphereViewModel viewModel)
            {
                if (sender is Button button)
                {
                    if (button.DataContext is JsonFacet facet)
                    {
                        viewModel.RemoveFacet(facet);
                        await viewModel.Save();
                    }
                }
            }
        }

        private async void AddFacetButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new();

            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Content = new AddFacetPage();
            dialog.PrimaryButtonText = "Add";
            dialog.SecondaryButtonText = "Cancel";

            dialog.Title = "Add Facet";

            ContentDialogResult facetAdded = await dialog.ShowAsync();

            if (facetAdded == ContentDialogResult.Primary)
            {
                if (dialog.Content is AddFacetPage addFacetPage)
                {
                    var dialogViewModel = addFacetPage.DataContext as AddFacetViewModel;
                    if (dialogViewModel != null)
                    {
                        var facet = new JsonFacet(dialogViewModel);
                        facet.Picon = await facet.GetIconAsync();
                        var sphereViewModel = DataContext as SphereViewModel;
                        sphereViewModel?.AddFacet(facet);
                    }
                }
            }

            if (facetAdded == ContentDialogResult.None)
            {
                dialog.Hide();
            }
        }

        // Replace the problematic line in ModifiersComboBox_SelectionChanged method
        private void ModifiersComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                if (comboBox.SelectedItem is VirtualKeyModifiers selectedModifier)
                {
                    if (DataContext is SphereViewModel viewModel)
                    {
                        viewModel.SelectModifier(selectedModifier);
                        viewModel.SelectKey(VirtualKey.None);
                    }
                }
            }
        }

        private void KeysComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                if (comboBox.SelectedItem is VirtualKey selectedKey)
                {
                    if (DataContext is SphereViewModel viewModel)
                    {
                        viewModel.SelectKey(selectedKey);
                    }
                }
            }

        }
    }
}
