// LiveSetBuilder.Core/Models/Song.cs
namespace LiveSetBuilder.Core.Models;

public sealed class Song
{
    public int Id { get; set; }
    public int ShowId { get; set; }
    public string Title { get; set; } = "";
    public double? Bpm { get; set; }
    public string? TimeSig { get; set; }
    public double StartGapBars { get; set; } = 0;
    public int OrderIndex { get; set; }
    public string? Notes { get; set; }
}
