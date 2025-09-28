// LiveSetBuilder.Core/Models/Asset.cs
namespace LiveSetBuilder.Core.Models;

public sealed class Asset
{
    public int Id { get; set; }
    public int SongId { get; set; }
    public AssetKind Kind { get; set; }
    public string SourcePath { get; set; } = "";
    public int SampleRate { get; set; }
    public int BitDepth { get; set; }
    public double DurationSec { get; set; }
    public string? MetadataJson { get; set; }
}
