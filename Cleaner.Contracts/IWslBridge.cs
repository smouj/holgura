namespace Cleaner.Contracts;

public sealed record WslDistro(string Name, string State, int Version);

public sealed record WslUsage(string Key, long Bytes, long FileCount);

/// <summary>Talks to WSL without ever composing a shell string from user input.</summary>
public interface IWslBridge
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WslDistro>> ListDistrosAsync(CancellationToken cancellationToken = default);

    /// <summary>Measures the known disposable locations inside a distro.</summary>
    Task<IReadOnlyList<WslUsage>> MeasureAsync(string distro, CancellationToken cancellationToken = default);

    /// <summary>Runs one whitelisted cleanup recipe. Returns the process exit code.</summary>
    Task<int> CleanAsync(string distro, string recipe, bool dryRun, CancellationToken cancellationToken = default);
}
