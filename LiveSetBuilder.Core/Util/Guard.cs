// LiveSetBuilder.Core/Util/Guard.cs
namespace LiveSetBuilder.Core.Util;

public static class Guard
{
    public static void NotNull(object? o, string name)
    {
        if (o is null) throw new ArgumentNullException(name);
    }

    public static void FileExists(string path, string name)
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"{name} not found", path);
    }
}
