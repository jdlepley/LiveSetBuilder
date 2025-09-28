using LiveSetBuilder.App.ViewModels;
using LiveSetBuilder.Core.Models;
using LiveSetBuilder.Core.Services;
using LiveSetBuilder.Core.Storage;
#if WINDOWS
using LiveSetBuilder.Platform.Audio;
#endif

namespace LiveSetBuilder.App.Pages;

public partial class SetEditorPage : ContentPage
{
    private SetEditorViewModel Vm => (SetEditorViewModel)BindingContext;
    private readonly SongRepository _songs;
    private readonly AssetRepository _assets;
    private readonly AnalysisService _analysis;
    private readonly IngestService _ingest;
#if WINDOWS
    private readonly IAudioPreview _preview;
#endif

    public SetEditorPage(SetEditorViewModel vm,
                         SongRepository songs,
                         AssetRepository assets,
                         AnalysisService analysis,
                         IngestService ingest
#if WINDOWS
                         , IAudioPreview preview
#endif
                         )
    {
        InitializeComponent();
        BindingContext = vm;
        _songs = songs;
        _assets = assets;
        _analysis = analysis;
        _ingest = ingest;
#if WINDOWS
        _preview = preview;
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Vm.LoadSetlistAsync();
    }

    private async void OnRunner(object? sender, EventArgs e)
    {
        if (Vm.Show != null)
            await Shell.Current.GoToAsync(nameof(RunnerPage), new Dictionary<string, object> { ["show"] = Vm.Show });
    }

    private async void OnAddSong(object? sender, EventArgs e)
    {
        if (Vm.Show == null) return;
        var order = Vm.Setlist.Count;
        var s = new Song { ShowId = Vm.Show.Id, Title = $"Song {order + 1}", OrderIndex = order, Bpm = null, TimeSig = "4/4" };
        s.Id = await _songs.InsertAsync(s);
        await Vm.LoadSetlistAsync();
        SetList.SelectedItem = Vm.Setlist.FirstOrDefault(x => x.Id == s.Id);
    }

    private async void OnImportMaster(object? sender, EventArgs e)
    {
        if (Vm.Show == null)
        {
            await DisplayAlert("No show", "Open or create a show first.", "OK");
            return;
        }

        var pick = await FilePicker.PickAsync(new PickOptions
        {
            PickerTitle = "Choose an audio file",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.WinUI,   new[] { ".wav", ".mp3", ".flac", ".aiff" } },
            { DevicePlatform.macOS,   new[] { ".wav", ".mp3", ".flac", ".aiff" } },
            { DevicePlatform.iOS,     new[] { "public.audio" } },
            { DevicePlatform.Android, new[] { "audio/*" } }
        })
        });
        if (pick == null) return;

        // Ensure there is a target song (create one if none selected)
        Song? song = SetList.SelectedItem as Song;
        if (song == null)
        {
            var titleFromFile = Path.GetFileNameWithoutExtension(pick.FileName);
            var order = Vm.Setlist.Count;
            song = new Song
            {
                ShowId = Vm.Show.Id,
                Title = titleFromFile,
                OrderIndex = order,
                TimeSig = "4/4"
            };
            song.Id = await _songs.InsertAsync(song);
            await Vm.LoadSetlistAsync();
            SetList.SelectedItem = Vm.Setlist.FirstOrDefault(x => x.Id == song.Id);
        }

        try
        {
            // Import and save asset
            var asset = await _ingest.ImportFileAsync(song.Id, pick.FullPath);
            asset.Id = await _assets.InsertAsync(asset);

            // Try BPM from metadata
            var bpm = await _analysis.ReadBpmFromMetadataAsync(asset.SourcePath);
            if (bpm.HasValue)
            {
                song.Bpm = bpm.Value;
                await _songs.UpdateAsync(song);
            }

            // Refresh UI and keep selection
            await Vm.LoadSetlistAsync();
            SetList.SelectedItem = Vm.Setlist.FirstOrDefault(x => x.Id == song.Id);

            await DisplayToastAsync($"Imported: {Path.GetFileName(pick.FullPath)}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Import failed", ex.Message, "OK");
        }
    }


    private async void OnDetectBpm(object? sender, EventArgs e)
    {
        if (SetList.SelectedItem is not Song song)
        {
            await DisplayAlert("Pick a song", "Select a song in the list first.", "OK");
            return;
        }
        var master = (await _assets.GetBySongAsync(song.Id)).FirstOrDefault(a => a.Kind == AssetKind.Master);
        if (master == null)
        {
            await DisplayAlert("No master", "Import a Master track first.", "OK");
            return;
        }

        // Try metadata first, then detection
        var bpm = await _analysis.ReadBpmFromMetadataAsync(master.SourcePath)
                  ?? await _analysis.DetectBpmAsync(master.SourcePath);
        song.Bpm = bpm;
        await _songs.UpdateAsync(song);
        await Vm.LoadSetlistAsync();
        await DisplayToastAsync($"BPM set to {bpm:0.#}");
    }

    private async void OnSongSelected(object? sender, SelectionChangedEventArgs e)
    {
        // Could load assets/waveform later
        await Task.CompletedTask;
    }

    private async void OnPlay(object? sender, EventArgs e)
    {
#if WINDOWS
        if (SetList.SelectedItem is not Song song)
        {
            await DisplayAlert("Pick a song", "Select a song in the list first.", "OK");
            return;
        }
        var master = (await _assets.GetBySongAsync(song.Id)).FirstOrDefault(a => a.Kind == AssetKind.Master);
        if (master == null) { await DisplayAlert("No master", "Import a Master track first.", "OK"); return; }
        await _preview.PlayAsync(master.SourcePath);
#else
        await DisplayAlert("Preview not available", "Audio preview is implemented on Windows. Mac/iOS coming next.", "OK");
#endif
    }

    private async void OnStop(object? sender, EventArgs e)
    {
#if WINDOWS
        await _preview.StopAsync();
#else
        await Task.CompletedTask;
#endif
    }

    private Task DisplayToastAsync(string message)
        => MainThread.IsMainThread
            ? Application.Current!.MainPage!.DisplayAlert("Live Set Builder", message, "OK")
            : MainThread.InvokeOnMainThreadAsync(() => Application.Current!.MainPage!.DisplayAlert("Live Set Builder", message, "OK"));
}
