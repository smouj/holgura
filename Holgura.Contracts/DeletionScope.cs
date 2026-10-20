namespace Holgura.Contracts;

public enum DeletionScope
{
    /// <summary>Regular Windows files, deleted through the file gate.</summary>
    FileSystem = 0,

    /// <summary>Handled by the module itself (for example inside a WSL distro).</summary>
    External = 1,
}
