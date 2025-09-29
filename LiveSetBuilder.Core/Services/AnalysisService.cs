using System.Diagnostics;

namespace LiveSetBuilder.Core.Services;

/// <summary>
/// BPM from metadata; if needed, detect via:
/// 1) ffmpeg decode → mono PCM16 WAV at low SR (default 11025 Hz)
/// 2) envelope from frame energy rise
/// 3) autocorrelation → BPM (40..240)
/// Supports progress + cancellation and early-exit if confidence is high.
/// </summary>
public sealed class AnalysisService
{
    private readonly string _ffmpegPath;   // "ffmpeg" if in PATH, or full path
    public AnalysisService(string ffmpegPath = "ffmpeg") => _ffmpegPath = ffmpegPath;

    public Task<double?> ReadBpmFromMetadataAsync(string path)
    {
        try
        {
            using var f = TagLib.File.Create(path);
            var bpm = (double)f.Tag.BeatsPerMinute;
            if (bpm >= 40 && bpm <= 260) return Task.FromResult<double?>(bpm);
        }
        catch { /* ignore */ }
        return Task.FromResult<double?>(null);
    }

    public Task<double?> DetectBpmAsync(string sourcePath, bool analyzeFullTrack,
                                        IProgress<double>? progress = null,
                                        System.Threading.CancellationToken ct = default)
        => DetectBpmAsync(sourcePath,
                          analyzeFullTrack ? (TimeSpan?)null : TimeSpan.FromSeconds(90), // quick by default
                          progress, ct);

    public async Task<double?> DetectBpmAsync(
        string sourcePath,
        TimeSpan? maxAnalyze,                                  // null => full track
        IProgress<double>? progress = null,
        System.Threading.CancellationToken ct = default,
        int targetSampleRate = 11025,                          // faster than 22.05k
        int frameSize = 2048,                                  // larger frame, better SNR
        int hopSize = 1024)                                    // larger hop => fewer frames
    {
        ct.ThrowIfCancellationRequested();

        int? limitSec = (maxAnalyze.HasValue && maxAnalyze.Value.TotalSeconds > 0)
                        ? (int)maxAnalyze.Value.TotalSeconds
                        : (int?)null;

        string tmp = Path.Combine(Path.GetTempPath(), $"lsb_bpm_{Guid.NewGuid():N}.wav");
        try
        {
            if (!await DecodeWithFfmpegAsync(sourcePath, tmp, targetSampleRate, limitSec, ct))
                return null;

            if (!TryReadPcm16MonoWav(tmp, out int sr, out float[] samples) || samples.Length < sr / 2)
                return null;

            // Envelope
            int frames = 1 + Math.Max(0, (samples.Length - frameSize) / hopSize);
            if (frames < 8) return null;

            var envelope = new float[frames];
            float prevEnergy = 0;
            int reported = 0;
            int reportEvery = Math.Max(1, frames / 100); // ~1% steps

            for (int i = 0, pos = 0; i < frames; i++, pos += hopSize)
            {
                ct.ThrowIfCancellationRequested();

                double energy = 0;
                int end = pos + frameSize;
                for (int n = pos; n < end; n++)
                {
                    float s = samples[n];
                    energy += s * s;
                }
                float e = (float)energy;
                float diff = e - prevEnergy;
                envelope[i] = diff > 0 ? diff : 0f;
                prevEnergy = e;

                // progress
                if (progress != null && (i - reported) >= reportEvery)
                {
                    reported = i;
                    progress.Report((double)i / frames);
                }

                // simple early-exit: every ~200 frames (~4.7s at 11025/1024), see if we already have strong periodicity
                if (i > 400 && (i % 200 == 0))
                {
                    var slice = envelope.Take(i).ToArray();
                    var conf = EstimateConfidence(slice, sr, hopSize);
                    if (conf.confidence >= 0.30) // threshold: tune from 0.25–0.35
                    {
                        // final snap & return
                        var snapped = SnapBpm(slice.ToArray(), (double)sr / hopSize, conf.bpm);
                        return Math.Round(snapped, 1);
                    }
                }
            }

            Normalize(envelope);
            Highpass(envelope, 0.01f);
            Smooth(envelope, 4);

            var (bpm, score) = BestTempoFromAutocorr(envelope, (double)sr / hopSize, 40, 240);
            if (score < 0.05) return null;
            bpm = SnapBpm(envelope, (double)sr / hopSize, bpm);

            progress?.Report(1.0);
            return Math.Round(bpm, 1);
        }
        catch (OperationCanceledException) { throw; }
        catch { return null; }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* ignore */ }
        }
    }

    private (double bpm, double confidence) EstimateConfidence(float[] env, int sr, int hop)
    {
        var ac = AutoCorrelationQuick(env);
        double envFs = (double)sr / hop;
        var (bpm, score) = BestTempoFromAutocorr(ac, envFs, 40, 240);
        return (bpm, score);
    }


    private async Task<bool> DecodeWithFfmpegAsync(string src, string dst, int sr, int? maxSec,
                                                   System.Threading.CancellationToken ct)
    {
        try
        {
            if (File.Exists(dst)) File.Delete(dst);
            var limit = maxSec.HasValue ? $"-t {maxSec.Value}" : "";
            var psi = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = $"-y -i \"{src}\" {limit} -ac 1 -ar {sr} -vn -f wav \"{dst}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            using var p = Process.Start(psi)!;
            // cooperative cancel: kill process if canceled
            using (ct.Register(() => { try { if (!p.HasExited) p.Kill(); } catch { } }))
            {
                await p.WaitForExitAsync();
            }
            return p.ExitCode == 0 && File.Exists(dst) && new FileInfo(dst).Length > 0;
        }
        catch { return false; }
    }

    // ---- WAV IO ----
    private static bool TryReadPcm16MonoWav(string path, out int sampleRate, out float[] samples)
    {
        sampleRate = 0;
        samples = Array.Empty<float>();

        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);

        if (br.ReadUInt32() != 0x46464952) return false; // "RIFF"
        br.ReadUInt32(); // size
        if (br.ReadUInt32() != 0x45564157) return false; // "WAVE"

        ushort audioFormat = 0, numChannels = 0, bitsPerSample = 0;
        uint sampleRateU = 0;
        int dataBytes = 0;
        long dataPos = 0;

        while (br.BaseStream.Position + 8 <= br.BaseStream.Length)
        {
            uint id = br.ReadUInt32();
            uint size = br.ReadUInt32();
            long next = br.BaseStream.Position + size;

            if (id == 0x20746d66) // "fmt "
            {
                audioFormat = br.ReadUInt16();
                numChannels = br.ReadUInt16();
                sampleRateU = br.ReadUInt32();
                br.ReadUInt32(); // byte rate
                br.ReadUInt16(); // block align
                bitsPerSample = br.ReadUInt16();
            }
            else if (id == 0x61746164) // "data"
            {
                dataBytes = (int)size;
                dataPos = br.BaseStream.Position;
            }
            br.BaseStream.Position = next;
        }

        if (audioFormat != 1 || numChannels != 1 || bitsPerSample != 16 || dataBytes <= 0) return false;

        sampleRate = (int)sampleRateU;
        br.BaseStream.Position = dataPos;

        int count = dataBytes / 2;
        samples = new float[count];
        for (int i = 0; i < count; i++) samples[i] = br.ReadInt16() / 32768f;
        return true;
    }

    // ---- Envelope/AC helpers ----
    private static void Normalize(float[] x)
    {
        var max = x.Max();
        if (max > 1e-9f) for (int i = 0; i < x.Length; i++) x[i] /= max;
    }
    private static void Smooth(float[] x, int win)
    {
        if (win <= 1) return;
        var y = new float[x.Length];
        int half = win / 2;
        for (int i = 0; i < x.Length; i++)
        {
            int i0 = Math.Max(0, i - half);
            int i1 = Math.Min(x.Length - 1, i + half);
            float acc = 0;
            for (int j = i0; j <= i1; j++) acc += x[j];
            y[i] = acc / (i1 - i0 + 1);
        }
        Array.Copy(y, x, x.Length);
    }
    private static void Highpass(float[] x, float alpha)
    {
        float prevY = 0, prevX = 0;
        for (int i = 0; i < x.Length; i++)
        {
            float y = alpha * (prevY + x[i] - prevX);
            prevY = y; prevX = x[i];
            x[i] = y;
        }
    }

    private static float[] AutoCorrelationQuick(ReadOnlySpan<float> x)
    {
        int n = x.Length;
        var ac = new float[n];
        for (int lag = 1; lag < n; lag++)
        {
            double s = 0;
            int end = n - lag;
            for (int i = 0; i < end; i++) s += x[i] * x[i + lag];
            ac[lag] = (float)s;
        }
        for (int i = 0; i < Math.Min(4, ac.Length); i++) ac[i] = 0;
        // normalize
        float max = 0;
        for (int i = 0; i < n; i++) if (ac[i] > max) max = ac[i];
        if (max > 1e-9f) for (int i = 0; i < n; i++) ac[i] /= max;
        return ac;
    }

    private static float[] AutoCorrelation(float[] x) => AutoCorrelationQuick(x);

    private static (double bpm, double score) BestTempoFromAutocorr(float[] ac, double envFs, int minBpm, int maxBpm)
    {
        int lagMin = (int)Math.Floor(60.0 * envFs / maxBpm);
        int lagMax = (int)Math.Ceiling(60.0 * envFs / minBpm);
        lagMin = Math.Max(lagMin, 2);
        lagMax = Math.Min(lagMax, ac.Length - 1);
        if (lagMax <= lagMin) return (double.NaN, 0);

        int bestLag = lagMin;
        float best = 0;
        for (int lag = lagMin; lag <= lagMax; lag++)
            if (ac[lag] > best) { best = ac[lag]; bestLag = lag; }

        double bpm = 60.0 * envFs / bestLag;
        return (bpm, best);
    }

    private static double SnapBpm(float[] envelope, double envFs, double bpm)
    {
        double[] cands = { bpm * 0.5, bpm, bpm * 2 };
        double bestScore = -1, bestBpm = bpm;

        foreach (var b in cands)
        {
            if (b < 40 || b > 260) continue;
            int per = (int)Math.Round(60.0 * envFs / b);
            if (per < 2) continue;
            double s = 0; int c = 0;
            for (int i = per; i < envelope.Length; i += per) { s += envelope[i]; c++; }
            var score = c > 0 ? s / c : 0;
            if (score > bestScore) { bestScore = score; bestBpm = b; }
        }
        return bestBpm;
    }
}
