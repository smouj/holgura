namespace Holgura.Core;

public static class PathUtil
{
    public static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is empty.", nameof(path));
        }

        var full = Path.GetFullPath(path.Trim());
        var root = Path.GetPathRoot(full);
        if (string.IsNullOrEmpty(root))
        {
            return full;
        }

        return full.Length > root.Length ? full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : full;
    }

    public static bool IsSamePath(string left, string right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);

    /// <summary>True when <paramref name="candidate"/> is strictly inside <paramref name="root"/>.</summary>
    public static bool IsUnder(string candidate, string root)
    {
        var c = Normalize(candidate);
        var r = Normalize(root);
        if (string.Equals(c, r, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var prefix = r.EndsWith(Path.DirectorySeparatorChar) ? r : r + Path.DirectorySeparatorChar;
        return c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>True when <paramref name="ancestor"/> contains <paramref name="path"/> at any depth.</summary>
    public static bool IsAncestorOf(string ancestor, string path) => IsUnder(path, ancestor);

    public static int Depth(string path)
    {
        var normalized = Normalize(path);
        var root = Path.GetPathRoot(normalized) ?? string.Empty;
        var rest = normalized[root.Length..];
        return rest.Length == 0 ? 0 : rest.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
