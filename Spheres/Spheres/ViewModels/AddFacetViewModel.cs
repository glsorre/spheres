using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Spheres.Models;


namespace Spheres.ViewModels;

public partial class AddFacetViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUrl))]
    public partial FacetType Type { get; set; }

    [ObservableProperty]
    public partial string Content { get; set; }

    public bool IsUrl
    {
        get => Type == FacetType.Url;
    }

    public IEnumerable<FacetType> FacetTypeValues => Enum.GetValues(typeof(FacetType)).Cast<FacetType>();

    public AddFacetViewModel()
    {
        Type = FacetType.App;
        Content = "";
    }
}
