using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Spheres.Models;
using Spheres.ViewModels;
using Vanara.Extensions.Reflection;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using WinRT;
using Application = Microsoft.UI.Xaml.Application;
using Button = Microsoft.UI.Xaml.Controls.Button;
using DragEventArgs = Microsoft.UI.Xaml.DragEventArgs;
using ListView = Microsoft.UI.Xaml.Controls.ListView;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Spheres.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AddPage : Page
    {
        public AddPage()
        {
            this.InitializeComponent();
            this.DataContext = new AddViewModel();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddViewModel viewModel)
            {
                await viewModel.Load();
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddViewModel viewModel)
            {
                await viewModel.Refresh();
            }
        }

        private void SpheresFacetsList_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation =
            (e.DataView.Contains("Type") && e.DataView.Contains("Content"))
               ? DataPackageOperation.Move : DataPackageOperation.None;
        }

        private async void SpheresFacetsList_Drop(object sender, DragEventArgs e)
        {
            if (sender is ListView listView)
            {
                if (e.DataView.Contains("Type") && e.DataView.Contains("Content"))
                {
                    var deferral = e.GetDeferral(); // Get a deferral to handle async operations
                    try
                    {
                        var type = await e.DataView.GetDataAsync("Type"); // Use GetDataAsync instead of GetData
                        var content = await e.DataView.GetDataAsync("Content"); // Use GetDataAsync instead of GetData
                        if (type is int && content is string contentString) // Ensure the data is of correct types
                        {
                            var viewModel = DataContext as AddViewModel;
                            var facet = viewModel?.runningFacets
                                .OfType<JsonFacet>() // Use OfType to filter items of type JsonFacet
                                .FirstOrDefault(x => x.Type == (FacetType)type && x.Content == contentString);
                            if (facet != null)
                            {
                                viewModel.swapRunningToSphere(facet); // Remove from running facets
                            }
                        }
                    }
                    finally
                    {
                        deferral.Complete(); // Complete the deferral
                    }
                }
            }
        }

        private void FacetTemplate_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            if (sender is FrameworkElement element)
            {
                if (element.DataContext is JsonFacet facet)
                {   
                    args.Data.SetData("Type", (int)facet.Type);
                    args.Data.SetData("Content", facet.Content);
                }
            }
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddViewModel viewModel)
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;

                List<JsonFacet> jsonFacets = new List<JsonFacet>();
                foreach (JsonFacet item in viewModel.sphereFacets)
                {
                    item.Picon = await item.GetIconAsync();
                    jsonFacets.Add(item);
                }

                Sphere sphere = new Sphere();
                sphere.Name = viewModel.sphereName;
                sphere.Description = viewModel.sphereDescription;
                sphere.Icon = viewModel.sphereIcon.Glyph;
                sphere.Facets = [..jsonFacets];
                sphere.Processes = [];
                await sphere.Save();
            }
        }

        private async void CollectTrayFacetsButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddViewModel viewModel)
            {
                viewModel.collectTrayFacets = !viewModel.collectTrayFacets;
                CollectTrayFacetsButton.Label = viewModel.collectTrayFacets ? "Hide tray apps" : "Show tray apps";
                await viewModel.Refresh();
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
                        var addViewModel = DataContext as AddViewModel;
                        addViewModel?.AddToSphere(facet);
                    }
                }
            }

            if (facetAdded == ContentDialogResult.None)
            {
                dialog.Hide();
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddViewModel viewModel)
            {
                if (sender is Button button)
                {
                    if (button.DataContext is JsonFacet facet)
                    {
                        viewModel.swapSphereToRunning(facet);
                    }
                    if (button.DataContext is JsonFacet jsonFacet)
                    {
                        viewModel.RemoveFromSphere(jsonFacet);
                    }
                }
            }
        }
    }
}
