// LiveSetBuilder.Core/Util/FfmpegLocator.cs
namespace LiveSetBuilder.Core.Util;

public sealed class FfmpegLocator
{
    public string FfmpegPath { get; }
    public FfmpegLocator(string? explicitPath = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath)) { FfmpegPath = explicitPath!; return; }
#if WINDOWS
        FfmpegPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LiveSetBuilder","tools","ffmpeg","win-x64","ffmpeg.exe");
#elif MACCATALYST
        FfmpegPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LiveSetBuilder","tools","ffmpeg","osx-arm64","ffmpeg");
#else
        FfmpegPath = "ffmpeg";
#endif
    }
}
