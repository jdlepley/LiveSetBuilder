// LiveSetBuilder.Core/Services/AnalysisService.cs
using TagLib;

namespace LiveSetBuilder.Core.Services;

public sealed class AnalysisService
{
    public Task<double?> ReadBpmFromMetadataAsync(string path)
    {
        try
        {
            using var f = TagLib.File.Create(path);
            if (f.Tag.BeatsPerMinute > 0) return Task.FromResult<double?>(f.Tag.BeatsPerMinute);
        }
        catch { /* ignore */ }
        return Task.FromResult<double?>(null);
    }

    public Task<double> DetectBpmAsync(string path)
    {
        // TODO: implement onset/autocorr or P/Invoke aubio
        return Task.FromResult(120.0);
    }
}
