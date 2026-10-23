using Holgura.Contracts;

namespace Holgura.Engine.Modules;

/// <summary>
/// Chromium and Firefox caches only. The filter is an allow-list over path segments, so cookies,
/// history, passwords, sessions and extensions can never be selected even if they sit next to a
/// cache folder.
/// </summary>
public sealed class BrowserCacheCleaner : DirectoryCleanerModule
{
    private static readonly string[] CacheSegments =
    [
        "Cache", "Code Cache", "GPUCache", "ShaderCache", "GrShaderCache",
        "GPUPersistentCache", "GraphiteDawnCache", "DawnCache", "cache2", "thumbnails", "startupCache",
    ];

    public override string Id => "apps.browsercache";

    public override string DisplayName => "Cachés de navegador";

    public BrowserCacheCleaner()
    {
        SafetyLevel = SafetyLevel.Safe;
    }

    protected override IEnumerable<ScanRoot> GetRoots(ScanContext context)
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        foreach (var (label, root) in EnumerateChromium(local))
        {
            yield return new ScanRoot(root, $"{label} — caché", "Solo caché. Cookies, historial y contraseñas quedan fuera del alcance.");
        }

        foreach (var (label, root) in EnumerateFirefox(roaming))
        {
            yield return new ScanRoot(root, $"{label} — caché", "Solo caché. El perfil de Firefox no se toca.");
        }
    }

    protected override bool Include(FileEntry file, ScanRoot root, DateTimeOffset asOfUtc) =>
        CacheSegments.Any(segment => file.FullName.Contains($"{Path.DirectorySeparatorChar}{segment}{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<(string Label, string Root)> EnumerateChromium(string local)
    {
        if (string.IsNullOrWhiteSpace(local))
        {
            yield break;
        }

        var browsers = new (string Label, string Folder)[]
        {
            ("Chrome", Path.Combine(local, "Google", "Chrome", "User Data")),
            ("Edge", Path.Combine(local, "Microsoft", "Edge", "User Data")),
            ("Brave", Path.Combine(local, "BraveSoftware", "Brave-Browser", "User Data")),
            ("Vivaldi", Path.Combine(local, "Vivaldi", "User Data")),
        };

        foreach (var (label, folder) in browsers)
        {
            if (!Directory.Exists(folder))
            {
                continue;
            }

            foreach (var profile in Directory.EnumerateDirectories(folder).Take(8))
            {
                var name = Path.GetFileName(profile);
                if (name is "System Profile" or "Guest Profile" || name.StartsWith('.'))
                {
                    continue;
                }

                yield return ($"{label} {name}", profile);
            }
        }
    }

    private static IEnumerable<(string Label, string Root)> EnumerateFirefox(string roaming)
    {
        if (string.IsNullOrWhiteSpace(roaming))
        {
            yield break;
        }

        var profiles = Path.Combine(roaming, "Mozilla", "Firefox", "Profiles");
        if (!Directory.Exists(profiles))
        {
            yield break;
        }

        foreach (var profile in Directory.EnumerateDirectories(profiles).Take(8))
        {
            yield return ("Firefox", profile);
        }
    }
}
