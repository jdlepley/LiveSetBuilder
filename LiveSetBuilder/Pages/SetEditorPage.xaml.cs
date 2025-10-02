#pragma warning disable CA1416
using LiveSetBuilder.App.ViewModels;          // <-- ensure this exists (SetEditorViewModel)
using LiveSetBuilder.Core.Models;
using LiveSetBuilder.Core.Services;
using LiveSetBuilder.Core.Storage;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using System.Diagnostics;

#if WINDOWS
using LiveSetBuilder.Platform.Audio;
#endif

namespace LiveSetBuilder.App.Pages
{
    // ----- Editor-side models (lightweight) -----
    public enum BankItemKind { Click, CountIn, Sample, Track }

    public sealed class BankItem
    {
        public string Name { get; set; } = "";
        public BankItemKind Kind { get; set; }
        public string? FilePath { get; set; }    // for samples/tracks
        public double DefaultBars { get; set; } = 4;
    }

    public sealed class Clip
    {
        public string Label { get; set; } = "";
        public int Lane { get; set; }            // 0..N-1
        public double StartBar { get; set; }     // bars
        public double Bars { get; set; }         // length in bars
        public string ColorHex { get; set; } = "#3A7AFE";
    }

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

        // Per-song in-memory project (persist later via SQLite if you want)
        private readonly Dictionary<int, ObservableCollection<Clip>> _songClips = new();

        // SoundBank bound to right panel
        public ObservableCollection<BankItem> SoundBank { get; } = new();

        // Timeline config
        private double _barPx = 28;              // pixels per bar (zoomable)
        private const int Subdivisions = 4;      // ticks per bar
        private const double LaneHeight = 66;    // px
        private int _laneCount = 0;              // 0 lanes until something is dropped
        private double TimelineWidthBars = 64;   // logical width (bars)

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

            // Bind SoundBank
            SoundBankView.BindingContext = this;
            SoundBankView.SetBinding(ItemsView.ItemsSourceProperty, new Binding(nameof(SoundBank), source: this));

            SizeChanged += (_, __) => RedrawRulerAndSurface();
            BuildDefaultSoundBank();

            // Initialize zoom UI
            ZoomSlider.Value = _barPx;
            ZoomLabel.Text = $"{_barPx:0} px/bar";
        }

        private void BuildDefaultSoundBank()
        {
            // Create some starter presets so the right panel isn’t empty.
            SoundBank.Clear();

            SoundBank.Add(new BankItem
            {
                Name = "Click — 4 Bars",
                Kind = BankItemKind.Click,
                DefaultBars = 4
            });

            SoundBank.Add(new BankItem
            {
                Name = "Count-In — 1 Bar",
                Kind = BankItemKind.CountIn,
                DefaultBars = 1
            });

            SoundBank.Add(new BankItem
            {
                Name = "Sample Pad — FX Hit",
                Kind = BankItemKind.Sample,
                DefaultBars = 2
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                await Vm.LoadSetlistAsync();
                SongListView.SelectedItem = Vm.Setlist.FirstOrDefault();
            }
            catch { }
        }

        // ============================================================
        // Layout: Zoom
        // ============================================================

        private void OnZoomChanged(object? sender, ValueChangedEventArgs e)
        {
            _barPx = Math.Max(8, e.NewValue);
            ZoomLabel.Text = $"{_barPx:0} px/bar";
            RedrawRulerAndSurface();
        }

        // ============================================================
        // Layout: Ruler + Surface
        // ============================================================

        private void RedrawRulerAndSurface()
        {
            DrawRuler();
            DrawLanes();
            RedrawClips();
        }

        private void DrawRuler()
        {
            RulerGrid.Children.Clear();
            var width = TimelineWidthBars * _barPx;
            RulerGrid.WidthRequest = width;

            var barStack = new Grid
            {
                ColumnSpacing = 0,
                RowDefinitions =
                {
                    new RowDefinition{Height = 18}, // numbers
                    new RowDefinition{Height = 10}  // ticks
                },
                WidthRequest = width,
                HeightRequest = 28,
                BackgroundColor = Color.FromRgb(34, 34, 34),
            };

            for (int b = 0; b < TimelineWidthBars; b++)
            {
                barStack.ColumnDefinitions.Add(new ColumnDefinition { Width = _barPx });

                // Bar number
                var lbl = new Label
                {
                    Text = (b + 1).ToString(),
                    FontSize = 12,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    TextColor = Colors.LightGray
                };
                Grid.SetRow(lbl, 0);
                Grid.SetColumn(lbl, b);
                barStack.Children.Add(lbl);

                // Subdivision ticks
                var tickGrid = new Grid { ColumnSpacing = 0 };
                for (int s = 0; s < Subdivisions; s++)
                    tickGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                for (int s = 0; s < Subdivisions; s++)
                {
                    var tick = new BoxView
                    {
                        HeightRequest = (s == 0) ? 8 : 4,
                        WidthRequest = 1,
                        Color = (s == 0) ? Color.FromRgb(140, 140, 140) : Color.FromRgb(90, 90, 90),
                        HorizontalOptions = LayoutOptions.Start,
                        VerticalOptions = LayoutOptions.Center
                    };
                    Grid.SetColumn(tick, s);
                    tickGrid.Children.Add(tick);
                }
                Grid.SetRow(tickGrid, 1);
                Grid.SetColumn(tickGrid, b);
                barStack.Children.Add(tickGrid);
            }

            RulerGrid.Children.Add(barStack);
        }

        private void DrawLanes()
        {
            // Left lane labels
            LaneLabels.Children.Clear();
            for (int i = 0; i < _laneCount; i++)
            {
                var label = new Label
                {
                    Text = $"Lane {i + 1}",
                    VerticalTextAlignment = TextAlignment.Center,
                    HeightRequest = LaneHeight - 8,
                    Margin = new Thickness(0, 4, 0, 4)
                };
                LaneLabels.Children.Add(label);
            }

            // Background stripes on the surface
            SurfaceGrid.Children.Clear();
            SurfaceGrid.RowDefinitions.Clear();

            var surfaceWidth = TimelineWidthBars * _barPx;
            var surfaceHeight = Math.Max(1, _laneCount) * LaneHeight + 8;

            SurfaceGrid.WidthRequest = surfaceWidth;
            SurfaceGrid.HeightRequest = surfaceHeight;

            for (int i = 0; i < _laneCount; i++)
            {
                SurfaceGrid.RowDefinitions.Add(new RowDefinition { Height = LaneHeight });
                var color = (i % 2 == 0) ? Color.FromRgb(37, 37, 37) : Color.FromRgb(32, 32, 32);
                var rowBg = new BoxView { BackgroundColor = color, CornerRadius = 6, Margin = new Thickness(0, 4) };
                Grid.SetRow(rowBg, i);
                SurfaceGrid.Children.Add(rowBg);
            }

            // Resize the overlay for clips (defined in XAML)
            ClipCanvas.WidthRequest = surfaceWidth;
            ClipCanvas.HeightRequest = surfaceHeight;
        }

        // Keep ruler and editor horizontally in sync
        private void OnEditorHScrolled(object? s, ScrolledEventArgs e) =>
            RulerScroll.ScrollToAsync(e.ScrollX, 0, false);

        private void OnRulerScrolled(object? s, ScrolledEventArgs e) =>
            EditorHScroll.ScrollToAsync(e.ScrollX, EditorVScroll.ScrollY, false);

        // ============================================================
        // SongList behavior
        // ============================================================

        private void OnSongSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (SongListView.SelectedItem is not Song song) return;

            if (!_songClips.TryGetValue(song.Id, out var clips))
            {
                clips = new ObservableCollection<Clip>();
                _songClips[song.Id] = clips;
            }

            _laneCount = (clips.Count == 0) ? 0 : (clips.Max(c => c.Lane) + 1);
            RedrawRulerAndSurface();
        }

        private async void OnMoveUp(object? sender, EventArgs e)
        {
            try
            {
                if ((sender as Button)?.BindingContext is not Song song) return;
                if (song.OrderIndex <= 0) return;

                var all = (await _songs.GetByShowAsync(song.ShowId)).OrderBy(s => s.OrderIndex).ToList();
                var above = all.FirstOrDefault(s => s.OrderIndex == song.OrderIndex - 1);
                if (above == null) return;

                (song.OrderIndex, above.OrderIndex) = (above.OrderIndex, song.OrderIndex);
                await _songs.UpdateAsync(song);
                await _songs.UpdateAsync(above);
                await Vm.LoadSetlistAsync();
                SongListView.SelectedItem = Vm.Setlist.FirstOrDefault(s => s.Id == song.Id);
            }
            catch { }
        }

        private async void OnMoveDown(object? sender, EventArgs e)
        {
            try
            {
                if ((sender as Button)?.BindingContext is not Song song) return;

                var all = (await _songs.GetByShowAsync(song.ShowId)).OrderBy(s => s.OrderIndex).ToList();
                if (song.OrderIndex >= all.Count - 1) return;

                var below = all.FirstOrDefault(s => s.OrderIndex == song.OrderIndex + 1);
                if (below == null) return;

                (song.OrderIndex, below.OrderIndex) = (below.OrderIndex, song.OrderIndex);
                await _songs.UpdateAsync(song);
                await _songs.UpdateAsync(below);
                await Vm.LoadSetlistAsync();
                SongListView.SelectedItem = Vm.Setlist.FirstOrDefault(s => s.Id == song.Id);
            }
            catch { }
        }

        private async void OnDeleteSong(object? sender, EventArgs e)
        {
            try
            {
                if ((sender as Button)?.BindingContext is not Song song) return;
                var confirm = await DisplayAlert($"Delete “{song.Title}”?", "This cannot be undone.", "Yes", "No");
                if (!confirm) return;

                await _songs.DeleteAsync(song.Id);
                _songClips.Remove(song.Id);

                // Re-index remaining songs
                var rest = (await _songs.GetByShowAsync(song.ShowId)).OrderBy(s => s.OrderIndex).ToList();
                for (int i = 0; i < rest.Count; i++) { rest[i].OrderIndex = i; await _songs.UpdateAsync(rest[i]); }

                await Vm.LoadSetlistAsync();
                SongListView.SelectedItem = Vm.Setlist.FirstOrDefault();
            }
            catch { }
        }

        // ============================================================
        // Run Set
        // ============================================================

        private async void OnRunner(object? sender, EventArgs e)
        {
            try
            {
                if (Vm.Show != null)
                    await Shell.Current.GoToAsync(nameof(RunnerPage), new Dictionary<string, object> { ["show"] = Vm.Show });
            }
            catch { }
        }

        // ============================================================
        // Add Song (+) with Time Signature
        // ============================================================

        private async void OnAddSong(object? sender, EventArgs e)
        {
            try
            {
                if (Vm.Show == null) { await DisplayAlert("No show", "Open or create a show first.", "OK"); return; }

                var name = await DisplayPromptAsync("Add Song", "Song name:", "Continue", "Cancel", "Untitled");
                if (string.IsNullOrWhiteSpace(name)) return;
                name = name.Trim();

                // Time signature
                var ts = await DisplayActionSheet("Time Signature", "Cancel", null, "4/4", "3/4", "6/8", "Custom…");
                if (ts == "Cancel" || ts == null) return;

                string timeSig = ts;
                if (ts == "Custom…")
                {
                    var numStr = await DisplayPromptAsync("Time Signature", "Numerator (beats per bar):", "OK", "Cancel", keyboard: Keyboard.Numeric);
                    var denStr = await DisplayPromptAsync("Time Signature", "Denominator (note value):", "OK", "Cancel", keyboard: Keyboard.Numeric);
                    if (!int.TryParse(numStr, out var num) || num <= 0 || !int.TryParse(denStr, out var den) || den <= 0)
                    { await DisplayAlert("Invalid", "Please enter positive integers.", "OK"); return; }
                    timeSig = $"{num}/{den}";
                }

                // Optional BPM
                var bpmStr = await DisplayPromptAsync("BPM (optional)", "Enter BPM or leave blank:", "OK", "Skip", keyboard: Keyboard.Numeric);
                double? bpm = null;
                if (double.TryParse(bpmStr, System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture, out var b) &&
                    b >= 40 && b <= 260)
                    bpm = b;

                var order = Vm.Setlist.Count;
                var song = new Song { ShowId = Vm.Show.Id, Title = name, OrderIndex = order, TimeSig = timeSig, Bpm = bpm ?? 0 };
                song.Id = await _songs.InsertAsync(song);

                await Vm.LoadSetlistAsync();
                SongListView.SelectedItem = Vm.Setlist.FirstOrDefault(x => x.Id == song.Id);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        // ============================================================
        // SoundBank (+)
        // ============================================================

        private async void OnAddBankItem(object? sender, EventArgs e)
        {
            var choice = await DisplayActionSheet("Add to SoundBank", "Cancel", null,
                                                  "Import Sample/Track", "New Click Preset", "New Count-in Preset");
            if (choice == "Import Sample/Track")
            {
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
                if (pick != null)
                {
                    SoundBank.Add(new BankItem
                    {
                        Name = System.IO.Path.GetFileNameWithoutExtension(pick.FileName),
                        Kind = BankItemKind.Sample,
                        FilePath = pick.FullPath,
                        DefaultBars = 4
                    });
                }
            }
            else if (choice == "New Click Preset")
            {
                SoundBank.Add(new BankItem { Name = "Click — Custom", Kind = BankItemKind.Click, DefaultBars = 4 });
            }
            else if (choice == "New Count-in Preset")
            {
                SoundBank.Add(new BankItem { Name = "Count-in — Custom", Kind = BankItemKind.CountIn, DefaultBars = 1 });
            }
        }

        // ============================================================
        // Drag from SoundBank only
        // ============================================================

        private void OnBankItemDragStarting(object? sender, DragStartingEventArgs e)
        {
            if ((sender as Element)?.BindingContext is not BankItem item) return;
            e.Data.Properties["type"] = "bank";
            e.Data.Properties["name"] = item.Name;
            e.Data.Properties["kind"] = item.Kind.ToString();
            e.Data.Properties["file"] = item.FilePath;
            e.Data.Properties["bars"] = item.DefaultBars;
            e.Data.Text = item.Name;
        }

        // ============================================================
        // Drop into Editor
        // ============================================================

        private void OnEditorDrop(object? sender, DropEventArgs e)
        {
            try
            {
                // Only accept SoundBank drops
                if (!e.Data.Properties.TryGetValue("type", out var typeObj) || (typeObj?.ToString() != "bank"))
                    return;

                if (SongListView.SelectedItem is not Song song) return;

                // ensure collection exists for this song
                if (!_songClips.TryGetValue(song.Id, out var clips))
                {
                    clips = new ObservableCollection<Clip>();
                    _songClips[song.Id] = clips;
                }

                var p = e.GetPosition(ClipCanvas);
                if (p == null) return;

                // If no lanes yet, create first lane
                if (_laneCount == 0) _laneCount = 1;

                var lane = Math.Clamp((int)(p.Value.Y / LaneHeight), 0, Math.Max(0, _laneCount - 1));
                var startBar = Math.Max(0, (p.Value.X) / _barPx);

                // If dropped below existing lanes, expand lane count
                while (lane >= _laneCount) _laneCount++;

                string label = (e.Data.Properties.TryGetValue("name", out var n) ? n?.ToString() : "Clip") ?? "Clip";
                var bars = (e.Data.Properties.TryGetValue("bars", out var b) && b is double d) ? Math.Max(1, d) : 4;

                var color = "#3A7AFE";
                if (e.Data.Properties.TryGetValue("kind", out var kindStr) && kindStr is string ks)
                {
                    color = ks switch
                    {
                        nameof(BankItemKind.Click) => "#2E7D32",
                        nameof(BankItemKind.CountIn) => "#8E24AA",
                        nameof(BankItemKind.Sample) => "#0097A7",
                        nameof(BankItemKind.Track) => "#EF6C00",
                        _ => "#3A7AFE"
                    };
                }

                clips.Add(new Clip { Label = label, Lane = lane, StartBar = startBar, Bars = bars, ColorHex = color });

                // reflect lane change + draw
                DrawLanes();
                RedrawClips();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OnEditorDrop] {ex}");
            }
        }

        // ============================================================
        // Draw clips on ClipCanvas (AbsoluteLayout)
        // ============================================================

        private void RedrawClips()
        {
            if (SongListView.SelectedItem is not Song song) return;
            if (!_songClips.TryGetValue(song.Id, out var clips)) return;

            ClipCanvas.Children.Clear();

            foreach (var c in clips)
            {
                var rect = new Rect(
                    c.StartBar * _barPx + 6,
                    c.Lane * LaneHeight + 8,
                    Math.Max(_barPx * c.Bars - 12, 18),
                    LaneHeight - 16);

                var box = new BoxView
                {
                    Color = Color.FromArgb(c.ColorHex),
                    CornerRadius = 8
                };
                AbsoluteLayout.SetLayoutBounds(box, rect);
                AbsoluteLayout.SetLayoutFlags(box, AbsoluteLayoutFlags.None);
                ClipCanvas.Children.Add(box);

                var label = new Label
                {
                    Text = c.Label,
                    FontSize = 12,
                    LineBreakMode = LineBreakMode.TailTruncation
                };
                AbsoluteLayout.SetLayoutBounds(label,
                    new Rect(rect.X + 8, rect.Y + 6, Math.Max(40, rect.Width - 16), rect.Height - 12));
                AbsoluteLayout.SetLayoutFlags(label, AbsoluteLayoutFlags.None);
                ClipCanvas.Children.Add(label);
            }
        }

        // ============================================================
        // Misc (stubs/placeholders)
        // ============================================================

        private void OnCancelAnalysis(object? s, EventArgs e) { /* overlay cancel if you use it */ }
    }
}
