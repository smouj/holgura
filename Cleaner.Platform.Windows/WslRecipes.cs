namespace Cleaner.Platform.Windows;

/// <summary>Only these commands can ever run inside a distro.</summary>
public static class WslRecipes
{
    public static string? Resolve(string key) => key switch
    {
        "apt" => "sudo -n apt-get clean",
        "journal" => "sudo -n journalctl --vacuum-size=100M",
        "pip" => "rm -rf \"$HOME/.cache/pip\"",
        "npm" => "rm -rf \"$HOME/.npm/_cacache\"",
        "uv" => "rm -rf \"$HOME/.cache/uv\"",
        _ => null,
    };
}
