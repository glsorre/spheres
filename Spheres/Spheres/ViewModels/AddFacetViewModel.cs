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
using System;


namespace Spheres.ViewModels;

public partial class AddFacetViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUrl))]
    public partial FacetType type { get; set; }

    [ObservableProperty]
    public partial string content { get; set; }

    public bool IsUrl
    {
        get => type == FacetType.Url;
    }

    public IEnumerable<FacetType> FacetTypeValues => Enum.GetValues(typeof(FacetType)).Cast<FacetType>();

    public AddFacetViewModel()
    {
        type = FacetType.App;
        content = "";
    }
}
