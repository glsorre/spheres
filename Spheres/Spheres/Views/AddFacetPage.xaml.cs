using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Spheres.ViewModels;
using Spheres.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Spheres.Views
{
    /// <summary>  
    /// An empty page that can be used on its own or navigated to within a Frame.  
    /// </summary>  
    public sealed partial class AddFacetPage : Page
    {
        public AddFacetPage()
        {
            this.InitializeComponent();
            DataContext = new AddFacetViewModel();
        }
        public async void ChooseButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddFacetViewModel viewModel)
            {
                string? content = null;

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);
                
                if (viewModel.Type == FacetType.App)
                {
                    var picker = new Windows.Storage.Pickers.FileOpenPicker();
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                    picker.FileTypeFilter.Add(".exe");
                    picker.FileTypeFilter.Add(".bat");
                    picker.FileTypeFilter.Add(".cmd");
                    var file = await picker.PickSingleFileAsync();
                    content = file?.Path;
                }
                else if (viewModel.Type == FacetType.Folder)
                {
                    var picker = new Windows.Storage.Pickers.FolderPicker();
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                    var folder = await picker.PickSingleFolderAsync();
                    content = folder?.Path;
                }
                else if (viewModel.Type == FacetType.File)
                {
                    var picker = new Windows.Storage.Pickers.FileOpenPicker();
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                    picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
                    picker.FileTypeFilter.Add("*");
                    var file = await picker.PickSingleFileAsync();
                    content = file?.Path;
                }

                viewModel.Content = content;
            }
        }
    }
}
