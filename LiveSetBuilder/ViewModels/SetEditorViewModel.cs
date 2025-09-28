using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveSetBuilder.Core.Models;
using LiveSetBuilder.Core.Storage;
using System.Collections.ObjectModel;

namespace LiveSetBuilder.App.ViewModels;

public partial class SetEditorViewModel : ObservableObject, IQueryAttributable
{
    private readonly SongRepository _songs;
    private readonly AssetRepository _assets;
    private readonly MixItemRepository _mix;

    [ObservableProperty] private Show? show;
    public ObservableCollection<Song> Setlist { get; } = new();

    public SetEditorViewModel(SongRepository songs, AssetRepository assets, MixItemRepository mix)
    {
        _songs = songs; _assets = assets; _mix = mix;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("show", out var s) && s is Show sh)
        {
            Show = sh;
            await LoadSetlistAsync();
        }
    }

    [RelayCommand]
    public async Task LoadSetlistAsync()
    {
        if (Show == null) return;
        Setlist.Clear();
        foreach (var s in await _songs.GetByShowAsync(Show.Id)) Setlist.Add(s);
    }
}
