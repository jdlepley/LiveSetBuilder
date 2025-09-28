using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveSetBuilder.Core.Models;
using LiveSetBuilder.Core.Storage;
using System.Collections.ObjectModel;

namespace LiveSetBuilder.App.ViewModels;

public partial class ShowsViewModel : ObservableObject
{
    private readonly ShowRepository _shows;
    private readonly SongRepository _songs;
    public ObservableCollection<Show> Items { get; } = new();

    public ShowsViewModel(ShowRepository shows, SongRepository songs)
    {
        _shows = shows; _songs = songs;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        Items.Clear();
        foreach (var s in await _shows.GetAllAsync()) Items.Add(s);
    }

    [RelayCommand]
    public async Task NewShowAsync()
    {
        var s = new Show { Title = "New Show" };
        s.Id = await _shows.InsertAsync(s);
        await LoadAsync();
    }

    [RelayCommand]
    public async Task<Show?> EnsureDemoAsync()
    {
        if (Items.Count == 0)
        {
            var show = new Show { Title = "Fall Tour 2025" };
            show.Id = await _shows.InsertAsync(show);
            await _songs.InsertAsync(new Song { ShowId = show.Id, Title = "Neon Skyline", OrderIndex = 0, Bpm = 120, TimeSig = "4/4" });
            await _songs.InsertAsync(new Song { ShowId = show.Id, Title = "Night Drive", OrderIndex = 1, Bpm = 92, TimeSig = "4/4" });
            await LoadAsync();
        }
        return Items.FirstOrDefault();
    }
}
