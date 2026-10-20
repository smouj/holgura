namespace Cleaner.Core;

/// <summary>
/// The last line of defence. Every destructive operation must pass through here, even if a module
/// hands over a path because of a bug. Windows drives, system folders and user document folders can
/// never be used as a deletion root, and no deletion may escape the declared root.
/// </summary>
public sealed class PathSafetyValidator
{
    /// <summary>Windows will not be asked to delete a tree shallower than this (e.g. "C:\Temp").</summary>
    private const int MinimumDepth = 1;

    private readonly string[] _protectedPaths;

    public PathSafetyValidator(IEnumerable<string> protectedPaths)
    {
        _protectedPaths = protectedPaths
            .Where(static p => !string.IsNullOrWhiteSpace(p))
            .Select(PathUtil.Normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<string> ProtectedPaths => _protectedPaths;

    public static PathSafetyValidator CreateDefault() => new(DefaultProtectedPaths());

    public static IEnumerable<string> DefaultProtectedPaths()
    {
        yield return Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
        yield return Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.System);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }

    /// <summary>Validates a directory a module wants to treat as a deletion root.</summary>
    public void EnsureSafeRoot(string root)
    {
        var path = PathUtil.Normalize(root);

        if (Path.GetPathRoot(path) is { Length: > 0 } drive && PathUtil.IsSamePath(path, drive))
        {
            throw new ProtectedPathException(path, "it is a drive root");
        }

        if (PathUtil.Depth(path) < MinimumDepth)
        {
            throw new ProtectedPathException(path, "the directory is too close to the drive root");
        }

        foreach (var guarded in _protectedPaths)
        {
            if (PathUtil.IsSamePath(path, guarded))
            {
                throw new ProtectedPathException(path, "it is a protected system or user folder");
            }

            if (PathUtil.IsAncestorOf(path, guarded))
            {
                throw new ProtectedPathException(path, $"it contains the protected path '{guarded}'");
            }
        }
    }

    /// <summary>Validates a single file against the root it was discovered under.</summary>
    public void EnsureContained(string root, string filePath)
    {
        EnsureSafeRoot(root);

        if (!PathUtil.IsUnder(filePath, root))
        {
            throw new ProtectedPathException(filePath, $"it escapes the allowed root '{root}'");
        }

        foreach (var guarded in _protectedPaths)
        {
            if (PathUtil.IsSamePath(filePath, guarded))
            {
                throw new ProtectedPathException(filePath, "it is a protected system or user folder");
            }
        }
    }

    public bool IsProtected(string path)
    {
        var normalized = PathUtil.Normalize(path);
        return _protectedPaths.Any(guarded => PathUtil.IsSamePath(normalized, guarded));
    }
}
