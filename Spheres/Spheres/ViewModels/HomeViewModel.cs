using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Collections;
using Spheres.Models;


namespace Spheres.ViewModels;

public partial class HomeViewModel : AppViewModel
{
    public HomeViewModel(AppViewModel appViewModel)
        : base(appViewModel.IsLoading, appViewModel.Spheres)
    {
    }
}
