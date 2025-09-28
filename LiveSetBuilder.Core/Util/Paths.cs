// LiveSetBuilder.Core/Util/Paths.cs
namespace LiveSetBuilder.Core.Util;

public static class Paths
{
    public static string AppData(string subdir)
    {
        var root = Environment.GetFolderPath(
#if MACCATALYST
            Environment.SpecialFolder.ApplicationData
#else
            Environment.SpecialFolder.LocalApplicationData
#endif
        );
        var p = Path.Combine(root, "LiveSetBuilder", subdir);
        Directory.CreateDirectory(p);
        return p;
    }
}
