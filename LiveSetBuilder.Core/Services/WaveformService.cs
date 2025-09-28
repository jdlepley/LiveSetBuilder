// LiveSetBuilder.Core/Services/WaveformService.cs
namespace LiveSetBuilder.Core.Services;

public sealed class WaveformService
{
    public Task<float[]> BuildPeaksAsync(string path, int samples)
    {
        // TODO: decode to mono peaks for UI (use ffmpeg -filter astats/silencedetect or custom decode)
        return Task.FromResult(Array.Empty<float>());
    }
}
