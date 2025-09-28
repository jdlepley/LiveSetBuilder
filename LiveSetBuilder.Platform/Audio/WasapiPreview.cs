// LiveSetBuilder.Platform/Audio/WasapiPreview.cs (WINDOWS ONLY stub)
#if WINDOWS
using NAudio.Wave;

namespace LiveSetBuilder.Platform.Audio;

public sealed class WasapiPreview : IAudioPreview
{
    private WaveOutEvent? _out;
    private AudioFileReader? _reader;

    public bool IsPlaying { get; private set; }

    public Task InitializeAsync()
    {
        _out = new WaveOutEvent();
        return Task.CompletedTask;
    }

    public async Task PlayAsync(string filePath)
    {
        await StopAsync();
        _reader = new AudioFileReader(filePath);
        _out!.Init(_reader);
        _out.Play();
        IsPlaying = true;
    }

    public Task StopAsync()
    {
        _out?.Stop();
        _reader?.Dispose(); _reader = null;
        IsPlaying = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _out?.Dispose();
        _reader?.Dispose();
        return ValueTask.CompletedTask;
    }
}
#endif
