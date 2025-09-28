// LiveSetBuilder.Core/Models/Show.cs
namespace LiveSetBuilder.Core.Models;

public sealed class Show
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public double? DefaultBpm { get; set; }
    public string? DefaultTimeSig { get; set; } // "4/4"
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
