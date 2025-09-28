// LiveSetBuilder.Core/Models/ExportConfig.cs
namespace LiveSetBuilder.Core.Models;

public sealed class ExportConfig
{
    public int Id { get; set; }
    public int ShowId { get; set; }
    public Target Target { get; set; } = Target.Ableton;
    public string PanningPreset { get; set; } = "ClickCueL_BandR";
    public int SampleRate { get; set; } = 48000;
    public double HeadroomDb { get; set; } = 6.0;
}
