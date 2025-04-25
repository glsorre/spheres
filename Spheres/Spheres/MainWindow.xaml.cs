using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using FlaUI.UIA3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Spheres.Models;
using Spheres.Views;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Spheres
{
    public sealed partial class MainWindow : Window
    {
        public Dictionary<string, Sphere> SavedSpheres { get; set; } = [];
        private FileSystemWatcher localFolderWatcher = new();
        public UIA3Automation automation { get; } = new();
        public MainWindow()
        {
            this.InitializeComponent();
            localFolderWatcher.Path = ApplicationData.Current.LocalFolder.Path;
            localFolderWatcher.NotifyFilter = NotifyFilters.FileName;
            localFolderWatcher.Filter = "sphere_*.json";

            localFolderWatcher.Renamed += LocalFolderWatcher_Renamed;

            localFolderWatcher.EnableRaisingEvents = true;

            this.Closed += MainWindow_Closed;

            this.NavView.Loaded += NavView_Loaded;
            this.NavView.SelectionChanged += NavView_SelectionChanged;
        }

        private async Task MainWindow_LoadSpheres()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            var files = await localFolder.GetFilesAsync();
            var sphereFiles = files.Where(file => file.Name.StartsWith("sphere_") && file.Name.EndsWith(".json"));
            sphereFiles = sphereFiles.OrderBy(file => file.Name);

            foreach (var file in sphereFiles)
            {
                using (var stream = await file.OpenStreamForReadAsync())
                {
                    Sphere? fileSphere = JsonSerializer.Deserialize<Sphere>(stream);
                    SavedSpheres.Add(fileSphere.Name, fileSphere);
                    AddNavViewItem(fileSphere.Name, fileSphere.Icon, typeof(SpherePage));
                    ((App)App.Current).AddTrayItem(fileSphere.Name);
                }
            }
        }

        private async void LocalFolderWatcher_Renamed(object sender, FileSystemEventArgs e)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile file = await localFolder.GetFileAsync(e.Name);

            using (var stream = await file.OpenStreamForReadAsync())
            {
                Sphere? fileSphere = JsonSerializer.Deserialize<Sphere>(stream);
                if (fileSphere != null)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        SavedSpheres.Add(fileSphere.Name, fileSphere);
                        AddNavViewItem(fileSphere.Name, fileSphere.Icon, typeof(SpherePage));
                        SetCurrentNavViewItem(GetNavViewItems(typeof(SpherePage), fileSphere.Name).FirstOrDefault());
                    });
                }
            }
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            localFolderWatcher.Dispose();
        }

        private async void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            await MainWindow_LoadSpheres();
            SetCurrentNavViewItem(GetNavViewItems(typeof(HomePage), "Dashboard").First());
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            SetCurrentNavViewItem(args.SelectedItemContainer as NavigationViewItem);
        }

        public NavigationViewItem GetCurrentNavigationViewItem() => NavView.SelectedItem as NavigationViewItem;

        public void SetCurrentNavViewItem(NavigationViewItem item)
        {
            if (item == null)
            {
                return;
            }

            if (item.Tag == null)
            {
                return;
            }

            if (item.Tag.ToString() == typeof(HomePage).FullName)
            {
                NavFrame.Navigate(typeof(HomePage), SavedSpheres.Values.ToArray());
            }
            else if (item.Tag.ToString() == typeof(SpherePage).FullName)
            {
                NavFrame.Navigate(typeof(SpherePage), SavedSpheres.GetValueOrDefault(item.Content.ToString()));
            }
            else
            {
                NavFrame.Navigate(Type.GetType(item.Tag.ToString()), item.Content);
            }
            NavView.SelectedItem = item;
        }

        public List<NavigationViewItem> GetNavViewItems()
        {
            var menuItems = NavView.MenuItems.OfType<NavigationViewItem>().ToList();
            var footerItems = NavView.FooterMenuItems.OfType<NavigationViewItem>().ToList();
            var allItems = new List<NavigationViewItem>();
            allItems.AddRange(menuItems);
            allItems.AddRange(footerItems);
            return allItems;
        }

        public List<NavigationViewItem> GetNavViewItems(Type type)
        {
            return GetNavViewItems().Where(i => i.Tag.ToString() == type.FullName).ToList();
        }

        public List<NavigationViewItem> GetNavViewItems(
            Type type,
            string title)
        {
            return GetNavViewItems(type).Where(ni => ni.Content.ToString() == title).ToList();
        }

        public void AddNavViewItem(
            string name,
            string icon,
            Type type
        )
        {
            var item = new NavigationViewItem()
            {
                Content = name,
                Tag = type.FullName,
                Icon = new FontIcon { Glyph = icon },
            };
            NavView.MenuItems.Add(item);
        }
    }
}