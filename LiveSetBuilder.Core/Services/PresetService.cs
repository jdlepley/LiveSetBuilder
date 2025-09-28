// LiveSetBuilder.Core/Services/PresetService.cs
namespace LiveSetBuilder.Core.Services;

public sealed class PresetService
{
    public record ClickFiles(string AccentPath, string RegularPath);
    public record ClickPreset(string Id, string Name, ClickFiles Files, double GainDb);
    public sealed class CountPreset
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public Dictionary<string, string> CountFiles { get; init; } = new(); // "1".."4"
        public int Bars { get; init; } = 1;
        public string Subdivision { get; init; } = "1/4";
        public double GainDb { get; init; } = 0;
    }

    private readonly Dictionary<string, ClickPreset> _clicks = new();
    private readonly Dictionary<string, CountPreset> _counts = new();

    private readonly Func<string, Task<string>> _assetCopier;
    public PresetService(Func<string, Task<string>> assetCopier /* inject EnsureAssetCopied */)
    {
        _assetCopier = assetCopier;
    }

    public async Task InitializeAsync()
    {
        // Example: load JSONs from assets after copying (implement your own loader)
        // var clickJsonPath = await _assetCopier("Presets/click_presets.json");
        // var countJsonPath = await _assetCopier("Presets/count_presets.json");
        // Deserialize and resolve file paths via _assetCopier for each referenced WAV.
    }

    public IEnumerable<ClickPreset> GetClickPresets() => _clicks.Values;
    public IEnumerable<CountPreset> GetCountPresets() => _counts.Values;
}
