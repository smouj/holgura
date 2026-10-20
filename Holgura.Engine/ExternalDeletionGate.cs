using Holgura.Contracts;

namespace Holgura.Engine;

/// <summary>
/// Passed to modules that delete through their own channel (a WSL distro, a shell API).
/// Any attempt to use it for a file deletion fails loudly.
/// </summary>
public sealed class ExternalDeletionGate : ICleanupGate
{
    public CleanupDecision Evaluate(string filePath) => CleanupDecision.Allow;

    public void Delete(string filePath) =>
        throw new InvalidOperationException("This module must perform its own deletion; the file gate does not apply.");
}
