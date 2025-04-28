using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Spheres.Models;

namespace Spheres.ViewModels
{
    public partial class AppViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial bool IsLoading { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<Sphere> Spheres { get; set; }

        [ObservableProperty]
        public partial Sphere? SelectedSphere { get; set; }

        public AppViewModel(bool isLoading, ObservableCollection<Sphere> spheres)
        {
            IsLoading = isLoading;
            Spheres = spheres;
            SelectedSphere = null;
        }

        public AppViewModel(bool isLoading, ObservableCollection<Sphere> spheres, Sphere selected)
        {
            IsLoading = isLoading;
            Spheres = spheres;
            SelectedSphere = selected;
        }
    }
}