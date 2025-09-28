// LiveSetBuilder.Core/Models/MixItem.cs
namespace LiveSetBuilder.Core.Models;

public sealed class MixItem
{
    public int Id { get; set; }
    public int SongId { get; set; }
    public int AssetId { get; set; }
    public double StartBeat { get; set; }      // beats from song start
    public double LengthBeats { get; set; }
    public double GainDb { get; set; } = 0;
    public double Pan { get; set; } = 0;       // -1..+1
    public MixRole Role { get; set; } = MixRole.Band;
}
