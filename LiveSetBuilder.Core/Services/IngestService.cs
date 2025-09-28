using LiveSetBuilder.Core.Models;
using System.Text.Json;

namespace LiveSetBuilder.Core.Services;

public sealed class IngestService
{
    public async Task<Asset> ImportFileAsync(int songId, string sourcePath)
    {
        if (!File.Exists(sourcePath)) throw new FileNotFoundException(sourcePath);

        // Copy to app cache
        var cacheRoot = Path.Combine(Environment.GetFolderPath(
#if MACCATALYST
            Environment.SpecialFolder.ApplicationData
#else
            Environment.SpecialFolder.LocalApplicationData
#endif
        ), "LiveSetBuilder", "media");
        Directory.CreateDirectory(cacheRoot);

        var destName = $"{Guid.NewGuid():N}{Path.GetExtension(sourcePath)}";
        var destPath = Path.Combine(cacheRoot, destName);
        using (var src = File.OpenRead(sourcePath))
        using (var dst = File.Create(destPath))
            await src.CopyToAsync(dst);

        // TagLib probe
        int sampleRate = 48000;
        int bitDepth = 16;
        double duration = 0;
        try
        {
            using var tf = TagLib.File.Create(destPath);
            sampleRate = tf.Properties.AudioSampleRate;
            duration = tf.Properties.Duration.TotalSeconds;
            // BitsPerSample may be 0 (unknown) on compressed formats
            bitDepth = tf.Properties.BitsPerSample > 0 ? tf.Properties.BitsPerSample : bitDepth;
        }
        catch { /* safe defaults retained */ }

        var meta = new { ImportedFrom = sourcePath, ImportedAtUtc = DateTime.UtcNow };
        return new Asset
        {
            SongId = songId,
            Kind = AssetKind.Master,
            SourcePath = destPath,
            SampleRate = sampleRate,
            BitDepth = bitDepth,
            DurationSec = duration,
            MetadataJson = JsonSerializer.Serialize(meta)
        };
    }
}
