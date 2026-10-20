namespace Cleaner.Contracts;

public readonly record struct FileEntry(string FullName, long Length, DateTime LastWriteUtc);
