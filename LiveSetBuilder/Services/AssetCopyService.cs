namespace LiveSetBuilder.App.Services;

public sealed class AssetCopyService
{
    public async Task<string> EnsureAssetCopiedAsync(string relativePathUnderResourcesRaw)
    {
        // relativePathUnderResourcesRaw like "Clicks/clave.wav"
        var outDir = Path.Combine(FileSystem.AppDataDirectory, "assets");
        Directory.CreateDirectory(outDir);

        var fileName = relativePathUnderResourcesRaw.Replace('/', '_');
        var dest = Path.Combine(outDir, fileName);

        if (!File.Exists(dest))
        {
            using var src = await FileSystem.OpenAppPackageFileAsync(relativePathUnderResourcesRaw);
            using var dst = File.Create(dest);
            await src.CopyToAsync(dst);
        }
        return dest;
    }
}
