namespace Holgura.Core;

/// <summary>Paths that Holgura must never expose to deletion, even if a module reports them.</summary>
public static class ProtectedPathPolicy
{
    public static bool IsProjectInfestPath(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        path.Contains("infest", StringComparison.OrdinalIgnoreCase);
}
