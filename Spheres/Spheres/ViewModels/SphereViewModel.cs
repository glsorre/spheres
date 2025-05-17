using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Xaml.Data;
using Spheres.Models;
using Spheres.Views;
using Windows.System;


namespace Spheres.ViewModels;

public class ModifiersToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is VirtualKeyModifiers modifiers)
        {
            return string.Join(" + ", Enum.GetValues(typeof(VirtualKeyModifiers))
                .Cast<VirtualKeyModifiers>()
                .Where(m => m != VirtualKeyModifiers.None && (modifiers & m) == m)
                .Select(m => m.ToString() ?? "None"));
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is string str)
        {
            var parts = str.Split(new[] { " + " }, StringSplitOptions.RemoveEmptyEntries);
            VirtualKeyModifiers modifiers = VirtualKeyModifiers.None;
            foreach (var part in parts)
            {
                if (Enum.TryParse(part, out VirtualKeyModifiers modifier))
                {
                    modifiers |= modifier;
                }
            }
            return modifiers;
        }
        return VirtualKeyModifiers.None;
    }
}

public class KeyToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is VirtualKey key)
        {
            return key.ToString();
        }
        return string.Empty;
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is string str)
        {
            if (Enum.TryParse(str, out VirtualKey key))
            {
                return key;
            }
        }
        return VirtualKey.None;
    }
}

public partial class SphereViewModel : AppViewModel
{
    public IReadOnlyList<VirtualKeyModifiers> Modifiers { get; } = new List<VirtualKeyModifiers>
    {
        VirtualKeyModifiers.None,
        VirtualKeyModifiers.Control | VirtualKeyModifiers.Windows,
        VirtualKeyModifiers.Menu | VirtualKeyModifiers.Windows,
        VirtualKeyModifiers.Shift | VirtualKeyModifiers.Windows,
        VirtualKeyModifiers.Control | VirtualKeyModifiers.Windows | VirtualKeyModifiers.Menu,
        VirtualKeyModifiers.Control | VirtualKeyModifiers.Windows | VirtualKeyModifiers.Shift,
    };

    public IReadOnlyList<VirtualKey> FunctionKeys { get; } = new List<VirtualKey>
    {
        VirtualKey.F1,
        VirtualKey.F2,
        VirtualKey.F3,
        VirtualKey.F4,
        VirtualKey.F5,
        VirtualKey.F6,
        VirtualKey.F7,
        VirtualKey.F8,
        VirtualKey.F9,
        VirtualKey.F10,
        VirtualKey.F11
    };

    public IReadOnlyList<VirtualKey> AllKeys { get; } = new List<VirtualKey>
    {
        VirtualKey.A,
        VirtualKey.B,
        VirtualKey.C,
        VirtualKey.D,
        VirtualKey.E,
        VirtualKey.F,
        VirtualKey.G,
        VirtualKey.H,
        VirtualKey.I,
        VirtualKey.J,
        VirtualKey.K,
        VirtualKey.L,
        VirtualKey.M,
        VirtualKey.N,
        VirtualKey.O,
        VirtualKey.P,
        VirtualKey.Q,
        VirtualKey.R,
        VirtualKey.S,
        VirtualKey.T,
        VirtualKey.U,
        VirtualKey.V,
        VirtualKey.W,
        VirtualKey.X,
        VirtualKey.Y,
        VirtualKey.Z,
        VirtualKey.Number0,
        VirtualKey.Number1,
        VirtualKey.Number2,
        VirtualKey.Number3,
        VirtualKey.Number4,
        VirtualKey.Number5,
        VirtualKey.Number6,
        VirtualKey.Number7,
        VirtualKey.Number8,
        VirtualKey.Number9,
        VirtualKey.F1,
        VirtualKey.F2,
        VirtualKey.F3,
        VirtualKey.F4,
        VirtualKey.F5,
        VirtualKey.F6,
        VirtualKey.F7,
        VirtualKey.F8,
        VirtualKey.F9,
        VirtualKey.F10,
        VirtualKey.F11
    };

    public IReadOnlyList<VirtualKey> Keys => SelectedSphere.Modifiers == VirtualKeyModifiers.None
        ? FunctionKeys
        : AllKeys;

    public SphereViewModel(AppViewModel appViewModel)
        : base(appViewModel.IsLoading, appViewModel.Spheres, appViewModel.SelectedSphere)
    {
    }

    public async void AddFacet(JsonFacet facet)
    {
        await SelectedSphere.AddFacet(facet);
    }

    public void RemoveFacet(JsonFacet facet)
    {
        SelectedSphere.RemoveFacet(facet);
    }

    public async Task Save()
    {
        await SelectedSphere.Save();
    }

    public async Task Delete()
    {
        App.m_window.DispatcherQueue.TryEnqueue(() =>
        {
            App.m_window.SetCurrentNavViewItem(App.m_window.GetNavViewItems(typeof(HomePage), "Dashboard").First());
            App.m_window.RemoveNavViewItem(typeof(SpherePage), SelectedSphere.Name);
        });
        await SelectedSphere.Delete();
        Spheres.Remove(SelectedSphere);
        SelectedSphere = null;
        OnPropertyChanged(nameof(SelectedSphere));
    }

    public void SelectModifier(VirtualKeyModifiers modifier)
    {
        SelectedSphere.SetModifiers(modifier);
        OnPropertyChanged(nameof(Keys));
    }

    public void SelectKey(VirtualKey key)
    {
        SelectedSphere.SetKey(key);
    }
}
