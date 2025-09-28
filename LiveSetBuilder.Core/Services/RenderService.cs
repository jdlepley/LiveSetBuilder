// LiveSetBuilder.Core/Services/RenderService.cs
namespace LiveSetBuilder.Core.Services;

public sealed class RenderService
{
    private readonly string _ffmpegPath;
    public RenderService(string ffmpegPath) => _ffmpegPath = ffmpegPath;

    public Task<string> RenderStereoShowAsync(int showId, object cfg, IProgress<double>? progress = null)
    {
        // TODO: build -filter_complex to route Click/Cues L and Band R, export WAV
        throw new NotImplementedException();
    }
}
