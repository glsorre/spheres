using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Collections;
using CommunityToolkit.WinUI.Converters;
using FlaUI.UIA3;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Spheres.Models;
using Spheres.ViewModels;
using Spheres.Views;
using Vanara.PInvoke;
using Windows.Storage;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Spheres
{
    public sealed partial class MainWindow : Window
    {
        private FileSystemWatcher localFolderWatcher = new();

        private readonly ComCtl32.SUBCLASSPROC WindowSubclassProc;

        public UIA3Automation automation { get; } = new();

        public AppViewModel AppViewModel { get; } = new(false, new ObservableCollection<Sphere>());

        public MainWindow()
        {
            WindowSubclassProc = new ComCtl32.SUBCLASSPROC(WindowSubclass);

            this.InitializeComponent();

            localFolderWatcher.Path = ApplicationData.Current.LocalFolder.Path;
            localFolderWatcher.NotifyFilter = NotifyFilters.FileName;
            localFolderWatcher.Filter = "sphere_*.json";

            localFolderWatcher.Renamed += LocalFolderWatcher_Renamed;

            localFolderWatcher.EnableRaisingEvents = true;

            this.Closed += MainWindow_Closed;

            this.NavView.Loaded += NavView_Loaded;
            this.NavView.SelectionChanged += NavView_SelectionChanged;

            HWND hWND = WindowNative.GetWindowHandle(this);
            ComCtl32.SetWindowSubclass(hWND, WindowSubclassProc, 0, 0);
        }

        public nint GetWindowHandle()
        {
            return WindowNative.GetWindowHandle(this);
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
                    SphereJsonConverter converter = new SphereJsonConverter();
                    var options = new JsonSerializerOptions();
                    var reader = new Utf8JsonReader(await File.ReadAllBytesAsync(file.Path));
                    Sphere? fileSphere = converter.Read(ref reader, typeof(Sphere), options);
                    await fileSphere.Init();
                    await fileSphere.Load();
                    AppViewModel.Spheres.Add(fileSphere);
                    AddNavViewItem(fileSphere.Name, fileSphere.Icon, typeof(SpherePage));
                    AddTrayItem(fileSphere);
                }
            }
        }

        public void AddTrayItem(Sphere sphere)
        {
            if (((App)App.Current).TrayIcon.ContextFlyout is MenuFlyout menuFlyout)
            {
                var checkedBinding = new Binding()
                {
                    Path = new PropertyPath("IsRunning"),
                    Mode = BindingMode.OneWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                var trayItem = new ToggleMenuFlyoutItem()
                {
                    Text = sphere.Name,
                    Command = (XamlUICommand)((App)App.Current).Resources["LaunchCloseSphereCommand"],
                    CommandParameter = sphere.Name,
                    DataContext = sphere
                };

                trayItem.SetBinding(ToggleMenuFlyoutItem.IsCheckedProperty, checkedBinding);

                menuFlyout.Items.Insert(0, trayItem);
            }
        }

        private async void LocalFolderWatcher_Renamed(object sender, FileSystemEventArgs e)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile file = await localFolder.GetFileAsync(e.Name);

            using (var stream = await file.OpenStreamForReadAsync())
            {
                SphereJsonConverter converter = new SphereJsonConverter();
                var options = new JsonSerializerOptions();
                var reader = new Utf8JsonReader(await File.ReadAllBytesAsync(file.Path));
                Sphere? fileSphere = converter.Read(ref reader, typeof(Sphere), options);
                await fileSphere.Init();
                if (fileSphere != null)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        AppViewModel.Spheres.Add(fileSphere);
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

        private nint WindowSubclass(HWND hWnd, uint uMsg, nint wParam, nint lParam, nuint uIdSubclass, nint dwRefData)
        {
            if (uMsg == (uint)User32.WindowMessage.WM_HOTKEY)
            {
                int hotkeyId = (int)wParam;
                var hotkeyedSphere = AppViewModel.Spheres.FirstOrDefault(s => s.GetHashCode() == hotkeyId);

                if (hotkeyedSphere != null)
                {
                    DispatcherQueue.TryEnqueue(async () =>
                    {
                        await hotkeyedSphere.Toggle();
                    });
                }
            }

            if (uMsg == (uint)User32.WindowMessage.WM_SYSCOMMAND && wParam.ToInt32() == (int)User32.SysCommand.SC_MINIMIZE)
            {
                this.Hide();
                return 0;
            }

            if (uMsg == (uint)User32.WindowMessage.WM_CLOSE)
            {
                this.Hide();
                return 0;
            }

            return ComCtl32.DefSubclassProc(hWnd, uMsg, wParam, lParam);
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
                // Pass reference to all spheres
                NavFrame.Navigate(typeof(HomePage), AppViewModel);
            }
            else if (item.Tag.ToString() == typeof(SpherePage).FullName)
            {
                AppViewModel.SelectedSphere = AppViewModel.Spheres.FirstOrDefault(s => s.Name == item.Content.ToString());
                NavFrame.Navigate(typeof(SpherePage), AppViewModel);
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

        public void RemoveNavViewItem(
            Type type,
            string name
        )
        {
            var item = GetNavViewItems(type, name).FirstOrDefault();
            if (item != null)
            {
                NavView.MenuItems.Remove(item);
            }
        }
    }
}