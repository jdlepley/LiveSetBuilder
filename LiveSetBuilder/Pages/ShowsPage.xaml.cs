using LiveSetBuilder.App.ViewModels;
using LiveSetBuilder.Core.Models;
using LiveSetBuilder.App.Pages;

namespace LiveSetBuilder.App.Pages;

public partial class ShowsPage : ContentPage
{
    private ShowsViewModel Vm => (ShowsViewModel)BindingContext;

    public ShowsPage(ShowsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Vm.LoadAsync();
    }

    private async void OnOpenEditor(object? sender, EventArgs e)
    {
        if (ShowsList.SelectedItem is Show s)
        {
            await Shell.Current.GoToAsync(nameof(SetEditorPage), new Dictionary<string, object> { ["show"] = s });
        }
        else
        {
            var demo = await Vm.EnsureDemoAsync();
            if (demo != null)
                await Shell.Current.GoToAsync(nameof(SetEditorPage), new Dictionary<string, object> { ["show"] = demo });
        }
    }
}
