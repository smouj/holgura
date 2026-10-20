using Cleaner.Contracts;

namespace Cleaner.Engine;

/// <summary>A directory a module considers disposable, plus the policy applied to it.</summary>
public sealed record ScanRoot(
    string Path,
    string Title,
    string Description,
    SafetyLevel SafetyLevel = SafetyLevel.Safe,
    bool SelectedByDefault = true,
    TimeSpan? MinimumAge = null,
    bool Recursive = true);
