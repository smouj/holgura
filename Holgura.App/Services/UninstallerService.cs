using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Holgura.App.Services;

public sealed class UninstallerService
{
    public Process Start(InstalledApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (application.IsProjectInfestProtected)
        {
            throw new InvalidOperationException("Project Infest está protegido y no se puede desinstalar desde Holgura.");
        }

        if (!TryCreateStartInfo(application.UninstallCommand, application.InstallLocation, out var startInfo, out var reason))
        {
            throw new InvalidOperationException(reason ?? "Windows no proporcionó un desinstalador utilizable.");
        }

        return Process.Start(startInfo) ?? throw new InvalidOperationException("Windows no pudo iniciar el desinstalador.");
    }

    internal static bool TryCreateStartInfo(
        string uninstallCommand,
        string installLocation,
        out ProcessStartInfo startInfo,
        out string? blockedReason)
    {
        startInfo = new ProcessStartInfo();
        blockedReason = null;
        var expandedCommand = Environment.ExpandEnvironmentVariables(uninstallCommand ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(expandedCommand))
        {
            blockedReason = "Windows no registró un comando de desinstalación.";
            return false;
        }

        if (!TrySplitCommandLine(expandedCommand, out var parts) || parts.Count == 0)
        {
            blockedReason = "El comando de desinstalación no se pudo interpretar de forma segura.";
            return false;
        }

        var executable = ResolveExecutable(parts[0], installLocation);
        if (executable is null)
        {
            blockedReason = "El ejecutable del desinstalador no está disponible.";
            return false;
        }

        var executableName = Path.GetFileName(executable);
        if (executableName.Equals("cmd.exe", StringComparison.OrdinalIgnoreCase) ||
            executableName.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase) ||
            executableName.Equals("pwsh.exe", StringComparison.OrdinalIgnoreCase) ||
            executableName.Equals("wscript.exe", StringComparison.OrdinalIgnoreCase) ||
            executableName.Equals("cscript.exe", StringComparison.OrdinalIgnoreCase) ||
            executableName.Equals("mshta.exe", StringComparison.OrdinalIgnoreCase))
        {
            blockedReason = "El desinstalador usa un intérprete de comandos; Holgura lo bloquea por seguridad. Desinstálalo desde Configuración de Windows.";
            return false;
        }

        var commandArguments = parts.Skip(1).ToArray();
        if (commandArguments.Any(IsSilentUninstallSwitch))
        {
            blockedReason = "El comando registrado intenta desinstalar en modo silencioso; Holgura lo bloquea para mantener visible el asistente.";
            return false;
        }

        if (executableName.Equals("msiexec.exe", StringComparison.OrdinalIgnoreCase))
        {
            commandArguments = NormalizeMsiUninstallArguments(commandArguments);
        }

        startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = string.Join(" ", commandArguments.Select(QuoteArgument)),
            WorkingDirectory = Directory.Exists(installLocation)
                ? installLocation
                : Path.GetDirectoryName(executable) ?? Environment.SystemDirectory,
            UseShellExecute = true,
        };

        return true;
    }

    private static bool IsSilentUninstallSwitch(string argument) => argument.Equals("/q", StringComparison.OrdinalIgnoreCase) ||
        argument.Equals("/qn", StringComparison.OrdinalIgnoreCase) ||
        argument.Equals("/quiet", StringComparison.OrdinalIgnoreCase) ||
        argument.Equals("/silent", StringComparison.OrdinalIgnoreCase) ||
        argument.Equals("/verysilent", StringComparison.OrdinalIgnoreCase) ||
        argument.Equals("/qn+", StringComparison.OrdinalIgnoreCase) ||
        argument.Equals("/suppressmsgboxes", StringComparison.OrdinalIgnoreCase) ||
        argument.Equals("/s", StringComparison.OrdinalIgnoreCase) ||
        argument.Equals("-silent", StringComparison.OrdinalIgnoreCase) ||
        argument.Equals("-s", StringComparison.OrdinalIgnoreCase);

    private static string[] NormalizeMsiUninstallArguments(string[] arguments)
    {
        var normalized = arguments.ToArray();
        for (var index = 0; index < normalized.Length; index++)
        {
            if (normalized[index].Equals("/I", StringComparison.OrdinalIgnoreCase) &&
                index + 1 < normalized.Length && Guid.TryParse(normalized[index + 1].Trim('{', '}'), out _))
            {
                normalized[index] = "/X";
            }
            else if (normalized[index].Length > 2 &&
                     normalized[index].StartsWith("/I{", StringComparison.OrdinalIgnoreCase) &&
                     Guid.TryParse(normalized[index][2..].Trim('{', '}'), out _))
            {
                normalized[index] = "/X" + normalized[index][2..];
            }
        }

        return normalized;
    }

    private static string? ResolveExecutable(string fileName, string installLocation)
    {
        var candidate = Environment.ExpandEnvironmentVariables(fileName.Trim().Trim('"'));
        if (Path.IsPathRooted(candidate))
        {
            return File.Exists(candidate) ? Path.GetFullPath(candidate) : null;
        }

        if (candidate.Equals("msiexec.exe", StringComparison.OrdinalIgnoreCase))
        {
            var systemMsiExec = Path.Combine(Environment.SystemDirectory, "msiexec.exe");
            return File.Exists(systemMsiExec) ? systemMsiExec : null;
        }

        if (Path.GetFileName(candidate).Equals(candidate, StringComparison.OrdinalIgnoreCase) &&
            Directory.Exists(installLocation))
        {
            var installedExecutable = Path.Combine(installLocation, candidate);
            if (File.Exists(installedExecutable))
            {
                return Path.GetFullPath(installedExecutable);
            }
        }

        return null;
    }

    private static bool TrySplitCommandLine(string commandLine, out IReadOnlyList<string> arguments)
    {
        arguments = [];
        var argumentVector = CommandLineToArgvW(commandLine, out var argumentCount);
        if (argumentVector == IntPtr.Zero || argumentCount <= 0)
        {
            return false;
        }

        try
        {
            var parsed = new string[argumentCount];
            for (var index = 0; index < argumentCount; index++)
            {
                var pointer = Marshal.ReadIntPtr(argumentVector, index * IntPtr.Size);
                parsed[index] = Marshal.PtrToStringUni(pointer) ?? string.Empty;
            }

            arguments = parsed;
            return true;
        }
        finally
        {
            _ = LocalFree(argumentVector);
        }
    }

    private static string QuoteArgument(string argument)
    {
        if (argument.Length > 0 && !argument.Any(char.IsWhiteSpace) && !argument.Contains('"'))
        {
            return argument;
        }

        var result = new StringBuilder("\"");
        var backslashes = 0;
        foreach (var character in argument)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }

            if (character == '"')
            {
                result.Append('\\', (backslashes * 2) + 1).Append('"');
                backslashes = 0;
                continue;
            }

            result.Append('\\', backslashes).Append(character);
            backslashes = 0;
        }

        result.Append('\\', backslashes * 2).Append('"');
        return result.ToString();
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CommandLineToArgvW")]
    private static extern IntPtr CommandLineToArgvW(string commandLine, out int argumentCount);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LocalFree(IntPtr memory);
}
