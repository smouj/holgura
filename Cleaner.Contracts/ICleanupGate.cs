namespace Cleaner.Contracts;

/// <summary>
/// The single choke point every deletion must pass through.
/// Implemented by the engine on top of <c>PathSafetyValidator</c> and the exclusion list.
/// </summary>
public interface ICleanupGate
{
    CleanupDecision Evaluate(string filePath);

    void Delete(string filePath);
}
