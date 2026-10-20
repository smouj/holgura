using System.Runtime.Versioning;

namespace Holgura.Contracts;

/// <summary>
/// Everything the engine touches goes through this interface, so tests can run against a fake tree.
/// </summary>
[UnsupportedOSPlatform("browser")]
public interface IFileSystem
{
    bool DirectoryExists(string path);

    bool FileExists(string path);

    IEnumerable<FileEntry> EnumerateFiles(string path, bool recursive);

    IEnumerable<string> EnumerateDirectories(string path);

    long GetFileLength(string path);

    DateTime GetLastWriteTimeUtc(string path);

    bool IsReparsePoint(string path);

    void DeleteFile(string path);

    void DeleteDirectory(string path, bool recursive);
}
