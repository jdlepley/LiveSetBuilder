// LiveSetBuilder.Platform/Audio/IAudioPreview.cs
namespace LiveSetBuilder.Platform.Audio;

public interface IAudioPreview : IAsyncDisposable
{
    Task InitializeAsync();
    Task PlayAsync(string filePath);
    Task StopAsync();
    bool IsPlaying { get; }
}
