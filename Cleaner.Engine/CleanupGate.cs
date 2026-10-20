using Cleaner.Contracts;
using Cleaner.Core;

namespace Cleaner.Engine;

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
        _validator.EnsureSafeRoot(root);
    }

    public CleanupDecision Evaluate(string filePath)
    {
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
        _validator.EnsureContained(_root, filePath);
        _fileSystem.DeleteFile(filePath);
    }
}
