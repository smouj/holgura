using Holgura.Contracts;

namespace Holgura.Engine.Tests;

/// <summary>A simulated tree so tests never touch a real disk.</summary>
public sealed class InMemoryFileSystem : IFileSystem
{
    private readonly Dictionary<string, FileEntry> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _reparsePoints = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _locked = new(StringComparer.OrdinalIgnoreCase);

    public void AddDirectory(string path) => _directories.Add(Trim(path));

    public void AddFile(string path, long length, DateTime? lastWriteUtc = null)
    {
        var normalized = Trim(path);
        _files[normalized] = new FileEntry(normalized, length, lastWriteUtc ?? DateTime.UtcNow.AddDays(-3));
        var parent = Path.GetDirectoryName(normalized);
        if (!string.IsNullOrEmpty(parent))
        {
            _directories.Add(parent);
        }
    }

    public void MarkReparsePoint(string path) => _reparsePoints.Add(Trim(path));

    public void MarkLocked(string path) => _locked.Add(Trim(path));

    public IReadOnlyCollection<string> Files => _files.Keys;

    public bool DirectoryExists(string path) => _directories.Contains(Trim(path));

    public bool FileExists(string path) => _files.ContainsKey(Trim(path));

    public IEnumerable<FileEntry> EnumerateFiles(string path, bool recursive)
    {
        var root = Trim(path);
        foreach (var (filePath, entry) in _files)
        {
            if (string.Equals(filePath, root, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var inside = filePath.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase);
            if (!inside)
            {
                continue;
            }

            if (!recursive)
            {
                var relative = filePath[(root.Length + 1)..];
                if (relative.Contains('\\'))
                {
                    continue;
                }
            }

            yield return entry;
        }
    }

    public IEnumerable<string> EnumerateDirectories(string path) => _directories.Where(d =>
        d.StartsWith(Trim(path) + "\\", StringComparison.OrdinalIgnoreCase));

    public long GetFileLength(string path) => _files.TryGetValue(Trim(path), out var entry) ? entry.Length : 0;

    public DateTime GetLastWriteTimeUtc(string path) => _files.TryGetValue(Trim(path), out var entry) ? entry.LastWriteUtc : DateTime.MinValue;

    public bool IsReparsePoint(string path) => _reparsePoints.Contains(Trim(path));

    public void DeleteFile(string path)
    {
        var normalized = Trim(path);
        if (_locked.Contains(normalized))
        {
            throw new IOException("The file is in use by another process.");
        }

        if (!_files.Remove(normalized))
        {
            throw new FileNotFoundException("Not found", normalized);
        }
    }

    public void DeleteDirectory(string path, bool recursive)
    {
        var prefix = Trim(path) + "\\";
        foreach (var key in _files.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            _files.Remove(key);
        }

        _directories.RemoveWhere(d => d.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        _directories.Remove(Trim(path));
    }

    private static string Trim(string path) => path.TrimEnd('\\');
}
