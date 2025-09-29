#pragma warning disable CA1416
using System.Collections.ObjectModel;
using LiveSetBuilder.Core.Models;
using LiveSetBuilder.Core.Storage;

namespace LiveSetBuilder.App.Pages;

public partial class ShowsPage : ContentPage
{
    // Pragmatic: manage the list here so we don’t depend on your VM’s shape.
    public ObservableCollection<Show> Shows { get; } = new();

    private readonly ShowRepository _showsRepo;

    public ShowsPage(ShowRepository showsRepo)
    {
        InitializeComponent();
        _showsRepo = showsRepo;

        // Bind this page’s property 'Shows' to the XAML ItemsSource
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var all = await _showsRepo.GetAllAsync();
        Shows.Clear();
        foreach (var s in all.OrderByDescending(s => s.UpdatedAt))
            Shows.Add(s);
    }

    private async void OnNewShow(object? sender, EventArgs e)
    {
        var name = await DisplayPromptAsync("New Show", "Name your show:", "Create", "Cancel");
        if (string.IsNullOrWhiteSpace(name)) return;

        var show = new Show { Title = name.Trim(), UpdatedAt = DateTime.UtcNow };
        show.Id = await _showsRepo.InsertAsync(show);

        await ReloadAsync();
        var created = Shows.FirstOrDefault(s => s.Id == show.Id);
        ShowList.SelectedItem = created;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // VisualStateManager on the template handles highlight automatically.
        // No code needed here.
    }

    private async void OnOpenSetEditorTapped(object? sender, TappedEventArgs e)
    {
        // Prefer the selected item if any, otherwise try the tapped context
        if (ShowList.SelectedItem is Show selected)
        {
            await Shell.Current.GoToAsync(nameof(SetEditorPage),
                new Dictionary<string, object> { ["show"] = selected });
            return;
        }

        if ((sender as View)?.BindingContext is Show tapped)
        {
            await Shell.Current.GoToAsync(nameof(SetEditorPage),
                new Dictionary<string, object> { ["show"] = tapped });
        }
    }

    private async void OnEditShow(object? sender, EventArgs e)
    {
        if ((sender as Button)?.BindingContext is not Show row) return;

        await Shell.Current.GoToAsync(nameof(SetEditorPage),
            new Dictionary<string, object> { ["show"] = row });
    }
}
