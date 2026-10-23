using Microsoft.Win32;
using Holgura.Core;
using System.IO;
using System.Security;

namespace Holgura.App.Services;

public sealed record InstalledApplication(
    string RegistryKey,
    string DisplayName,
    string Version,
    string Publisher,
    string InstallLocation,
    string UninstallCommand,
    long? EstimatedSizeBytes,
    string Scope,
    bool IsProjectInfestProtected,
    bool CanUninstall,
    string? UninstallBlockedReason)
{
    public string SizeText => EstimatedSizeBytes is > 0
        ? Holgura.Core.SizeFormatter.Format(EstimatedSizeBytes.Value)
        : "Tamaño no informado";

    public string VersionText => string.IsNullOrWhiteSpace(Version) ? "Versión no informada" : $"Versión {Version}";

    public string PublisherText => string.IsNullOrWhiteSpace(Publisher) ? "Editor no informado" : Publisher;
}

public sealed record InstalledAppsScanResult(
    IReadOnlyList<InstalledApplication> Applications,
    IReadOnlyList<string> Warnings);

public sealed class InstalledAppsService
{
    private const string UninstallKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    public InstalledAppsScanResult ReadInstalledApplications()
    {
        var applications = new List<InstalledApplication>();
        var warnings = new List<string>();
        var views = Environment.Is64BitOperatingSystem
            ? new[] { RegistryView.Registry64, RegistryView.Registry32 }
            : new[] { RegistryView.Registry32 };
        var hives = new[]
        {
            (RegistryHive.LocalMachine, "Equipo"),
            (RegistryHive.CurrentUser, "Este usuario"),
        };

        foreach (var (hive, scope) in hives)
        {
            foreach (var view in views)
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                    using var uninstallKey = baseKey.OpenSubKey(UninstallKeyPath, writable: false);
                    if (uninstallKey is null)
                    {
                        continue;
                    }

                    foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                    {
                        try
                        {
                            using var appKey = uninstallKey.OpenSubKey(subKeyName, writable: false);
                            if (appKey is null || IsHiddenSystemEntry(appKey))
                            {
                                continue;
                            }

                            var displayName = ReadString(appKey, "DisplayName");
                            if (string.IsNullOrWhiteSpace(displayName))
                            {
                                continue;
                            }

                            var installLocation = ReadString(appKey, "InstallLocation");
                            var uninstallCommand = ReadString(appKey, "UninstallString");
                            var keyPath = $"{hive}\\{view}\\{UninstallKeyPath}\\{subKeyName}";
                            var isProtected = ProtectedPathPolicy.IsProjectInfestPath(displayName) ||
                                              ProtectedPathPolicy.IsProjectInfestPath(installLocation) ||
                                              ProtectedPathPolicy.IsProjectInfestPath(uninstallCommand) ||
                                              ProtectedPathPolicy.IsProjectInfestPath(keyPath);
                            string? blockedReason = isProtected ? "Protegido por la regla de Project Infest." : null;
                            var canUninstall = !isProtected && UninstallerService.TryCreateStartInfo(
                                uninstallCommand,
                                installLocation,
                                out _,
                                out blockedReason);

                            applications.Add(new InstalledApplication(
                                keyPath,
                                displayName.Trim(),
                                ReadString(appKey, "DisplayVersion"),
                                ReadString(appKey, "Publisher"),
                                installLocation,
                                uninstallCommand,
                                ReadEstimatedSizeBytes(appKey),
                                scope,
                                isProtected,
                                canUninstall,
                                blockedReason));
                        }
                        catch (SecurityException)
                        {
                            warnings.Add($"Windows no permitió leer la entrada '{subKeyName}'.");
                        }
                        catch (IOException)
                        {
                            warnings.Add($"La entrada '{subKeyName}' cambió durante el análisis.");
                        }
                        catch (UnauthorizedAccessException)
                        {
                            warnings.Add($"Windows no permitió leer la entrada '{subKeyName}'.");
                        }
                    }
                }
                catch (SecurityException)
                {
                    warnings.Add($"No se pudo leer el registro de {scope} ({view}).");
                }
                catch (IOException)
                {
                    warnings.Add($"No se pudo completar la lectura del registro de {scope} ({view}).");
                }
                catch (UnauthorizedAccessException)
                {
                    warnings.Add($"No se pudo leer el registro de {scope} ({view}).");
                }
            }
        }

        var distinct = applications
            .GroupBy(app => $"{app.DisplayName.Trim()}|{app.Publisher.Trim()}|{app.Version.Trim()}|{app.UninstallCommand.Trim()}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(app => app.EstimatedSizeBytes.HasValue).First())
            .OrderBy(app => app.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        return new InstalledAppsScanResult(distinct, warnings.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static bool IsHiddenSystemEntry(RegistryKey appKey)
    {
        if (ReadInt64(appKey, "SystemComponent") == 1 ||
            !string.IsNullOrWhiteSpace(ReadString(appKey, "ParentKeyName")))
        {
            return true;
        }

        var releaseType = ReadString(appKey, "ReleaseType");
        return releaseType.Contains("update", StringComparison.OrdinalIgnoreCase) ||
               releaseType.Contains("hotfix", StringComparison.OrdinalIgnoreCase);
    }

    private static long? ReadEstimatedSizeBytes(RegistryKey appKey)
    {
        var value = ReadInt64(appKey, "EstimatedSize");
        if (value <= 0)
        {
            return null;
        }

        return value > long.MaxValue / 1024 ? long.MaxValue : value * 1024;
    }

    private static long ReadInt64(RegistryKey key, string name)
    {
        try
        {
            return Convert.ToInt64(key.GetValue(name, 0L), System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            return 0;
        }
        catch (InvalidCastException)
        {
            return 0;
        }
        catch (OverflowException)
        {
            return 0;
        }
    }

    private static string ReadString(RegistryKey key, string name)
    {
        try
        {
            return Convert.ToString(key.GetValue(name, string.Empty), System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        }
        catch (Exception ex) when (ex is IOException or SecurityException or UnauthorizedAccessException or InvalidCastException)
        {
            return string.Empty;
        }
    }
}
