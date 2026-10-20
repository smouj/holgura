namespace Holgura.Contracts;

public readonly record struct RecycleBinStats(long ItemCount, long TotalBytes);

/// <summary>The shell's recycle bin lives outside the file gate, so it gets its own contract.</summary>
public interface IRecycleBin
{
    RecycleBinStats Query();

    /// <summary>Empties the bin. Returns <c>true</c> when the shell reported success.</summary>
    bool Empty();
}
