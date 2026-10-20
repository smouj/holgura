namespace Holgura.Core;

public sealed class ProtectedPathException : Exception
{
    public ProtectedPathException(string path, string reason)
        : base($"Refusing to operate on '{path}': {reason}")
    {
        Path = path;
        Reason = reason;
    }

    public string Path { get; }

    public string Reason { get; }
}
