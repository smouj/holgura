using Cleaner.Contracts;

namespace Cleaner.Platform.Windows;

/// <summary>
/// Real file system access. Directory walks never follow reparse points, so a junction cannot
/// pull a deletion outside the root it was discovered under.
/// </summary>
public sealed class WindowsFileSystem : IFileSystem
{
    public bool DirectoryExists(string path)
    {
        try
        {
            return Directory.Exists(path);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool FileExists(string path)
    {
        try
        {
            return File.Exists(path);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public IEnumerable<FileEntry> EnumerateFiles(string path, bool recursive)
    {
        if (!DirectoryExists(path))
        {
            yield break;
        }

        var pending = new Stack<string>();
        pending.Push(path);

        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            FileSystemInfo[] entries;

            try
            {
                entries = new DirectoryInfo(directory).GetFileSystemInfos();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
            {
                continue;
            }

            foreach (var entry in entries)
            {
                if (entry is FileInfo file)
                {
                    long length;
                    DateTime lastWrite;

                    try
                    {
                        length = file.Length;
                        lastWrite = file.LastWriteTimeUtc;
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                    {
                        continue;
                    }

                    yield return new FileEntry(file.FullName, length, lastWrite);
                }
                else if (entry is DirectoryInfo child && recursive && !IsReparsePoint(child.FullName))
                {
                    pending.Push(child.FullName);
                }
            }
        }
    }

    public IEnumerable<string> EnumerateDirectories(string path)
    {
        if (!DirectoryExists(path))
        {
            return [];
        }

        try
        {
            return Directory.EnumerateDirectories(path);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return [];
        }
    }

    public long GetFileLength(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public DateTime GetLastWriteTimeUtc(string path)
    {
        try
        {
            return File.GetLastWriteTimeUtc(path);
        }
        catch (Exception)
        {
            return DateTime.MinValue;
        }
    }

    public bool IsReparsePoint(string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            return (attributes & FileAttributes.ReparsePoint) != 0;
        }
        catch (Exception)
        {
            // If we cannot prove it is a plain file, treat it as unsafe.
            return true;
        }
    }

    public void DeleteFile(string path)
    {
        var attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReadOnly) != 0)
        {
            File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
        }

        File.Delete(path);
    }

    public void DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);
}
