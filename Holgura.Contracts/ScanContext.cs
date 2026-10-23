namespace Holgura.Contracts;

public sealed class ScanContext
{
    public required IFileSystem FileSystem { get; init; }

    /// <summary>Maximum number of paths kept per item for the preview list.</summary>
    public int MaxSampleFiles { get; init; } = 100;

    /// <summary>Maximum number of file records retained per item for individual selection.</summary>
    public int MaxFileDetails { get; init; } = 2_000;

    /// <summary>Minimum age a file must have to be reported by age-filtered modules.</summary>
    public TimeSpan MinimumAge { get; init; } = TimeSpan.Zero;

    public string TempPath { get; init; } = Path.GetTempPath();
}
