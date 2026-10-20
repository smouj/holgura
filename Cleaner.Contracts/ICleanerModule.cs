namespace Cleaner.Contracts;

public interface ICleanerModule
{
    string Id { get; }

    string DisplayName { get; }

    ModuleCategory Category { get; }

    SafetyLevel SafetyLevel { get; }

    Task<ScanResult> ScanAsync(ScanContext context, CancellationToken cancellationToken);

    Task<CleanupResult> CleanAsync(CleanupContext context, CancellationToken cancellationToken);
}
