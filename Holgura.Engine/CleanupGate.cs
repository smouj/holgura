using Holgura.Contracts;
using Holgura.Core;

namespace Holgura.Engine;

/// <summary>
/// The only object allowed to delete. It re-checks the root, the exclusions, the file's existence
/// and whether it is a reparse point, immediately before touching the disk.
/// </summary>
public sealed class CleanupGate : ICleanupGate
{
    private readonly PathSafetyValidator _validator;
    private readonly ExclusionService _exclusions;
    private readonly IFileSystem _fileSystem;
    private readonly string _root;

    public CleanupGate(PathSafetyValidator validator, ExclusionService exclusions, IFileSystem fileSystem, string root)
    {
        _validator = validator;
        _exclusions = exclusions;
        _fileSystem = fileSystem;
        _root = root;

        if (ProtectedPathPolicy.IsProjectInfestPath(root))
        {
            throw new ProtectedPathException(root, "Project Infest paths are protected");
        }

        _validator.EnsureSafeRoot(root);
    }

    public CleanupDecision Evaluate(string filePath)
    {
        if (ProtectedPathPolicy.IsProjectInfestPath(filePath))
        {
            return CleanupDecision.Excluded;
        }

        try
        {
            _validator.EnsureContained(_root, filePath);
        }
        catch (ProtectedPathException)
        {
            return CleanupDecision.OutsideAllowedRoot;
        }

        if (_exclusions.IsExcluded(filePath))
        {
            return CleanupDecision.Excluded;
        }

        if (!_fileSystem.FileExists(filePath))
        {
            return CleanupDecision.NotFound;
        }

        if (_fileSystem.IsReparsePoint(filePath))
        {
            return CleanupDecision.ReparsePoint;
        }

        return CleanupDecision.Allow;
    }

    public void Delete(string filePath)
    {
        var decision = Evaluate(filePath);
        if (decision != CleanupDecision.Allow)
        {
            throw new CleanupRejectedException(decision);
        }

        _fileSystem.DeleteFile(filePath);
    }
}

internal sealed class CleanupRejectedException(CleanupDecision decision)
    : InvalidOperationException($"Deletion was rejected by the safety gate: {decision}.")
{
    public CleanupDecision Decision { get; } = decision;
}
