using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CommunityToolkit.WinUI.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using FlaUI.UIA3.Converters;
using FlaUI.UIA3.Patterns;
using FuzzySharp;
using Spheres.Models;
using Vanara.Extensions.Reflection;
using Vanara.PInvoke;
using Windows.Devices.I2c;
using Windows.System;
using static Vanara.PInvoke.ComCtl32;
using Process = System.Diagnostics.Process;
using User32 = Vanara.PInvoke.User32;
using System.Threading.Tasks;
using System;
using CommunityToolkit.Mvvm.Input;
using System.Security.Principal;
using static Vanara.PInvoke.Kernel32.PSS_HANDLE_ENTRY;
using CommunityToolkit.Common;


namespace Spheres.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Spheres))]
    public partial bool isLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Spheres))]
    public partial AdvancedCollectionView Spheres { get; set; }


    public HomeViewModel(Sphere[] spheres)
    {
        isLoading = true;
        Spheres = new AdvancedCollectionView(new ObservableCollection<Sphere>(spheres));
        Spheres.SortDescriptions.Add(new(nameof(Sphere.Name), SortDirection.Ascending));
    }
}
