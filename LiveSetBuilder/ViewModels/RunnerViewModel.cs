using CommunityToolkit.Mvvm.ComponentModel;
using LiveSetBuilder.Core.Models;

namespace LiveSetBuilder.App.ViewModels;

public partial class RunnerViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty] private Show? show;
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("show", out var s) && s is Show sh) Show = sh;
    }
}
