using System.Diagnostics;
using System.Text;
using Holgura.Contracts;

namespace Holgura.Platform.Windows;

/// <summary>
/// Runs WSL through argument lists (never a shell string), decodes its UTF-16/UTF-8 output and
/// keeps a hard timeout so a stuck distro cannot freeze the scan.
/// </summary>
public sealed class WslCliBridge : IWslBridge
{
    private static readonly TimeSpan ScanTimeout = TimeSpan.FromMinutes(3);

    private const string MeasureScript = """
        set -u
        emit() {
          d="$1"; k="$2"
          [ -d "$d" ] || return 0
          out=$(find "$d" -xdev -type f -printf '%s\n' 2>/dev/null | awk '{s+=$1;n++} END {printf "%d %d", s+0, n+0}')
          echo "$k|$out"
        }
        emit "/var/cache/apt/archives" "apt"
        emit "/var/log/journal" "journal"
        emit "$HOME/.cache/pip" "pip"
        emit "$HOME/.npm/_cacache" "npm"
        emit "$HOME/.cache/uv" "uv"
        """;

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var (exit, output, _) = await RunAsync(["--list", "--quiet"], cancellationToken).ConfigureAwait(false);
            return exit == 0 && !string.IsNullOrWhiteSpace(output);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<WslDistro>> ListDistrosAsync(CancellationToken cancellationToken = default)
    {
        var (exit, output, _) = await RunAsync(["--list", "--verbose"], cancellationToken).ConfigureAwait(false);
        if (exit != 0 || string.IsNullOrWhiteSpace(output))
        {
            return [];
        }

        var distros = new List<WslDistro>();
        foreach (var raw in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim().TrimStart('*').Trim();
            if (line.Length == 0 || line.StartsWith("NAME", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && int.TryParse(parts[^1], out var version))
            {
                distros.Add(new WslDistro(parts[0], string.Join(' ', parts[1..^1]), version));
            }
            else if (parts.Length > 0)
            {
                distros.Add(new WslDistro(parts[0], "Unknown", 2));
            }
        }

        return distros;
    }

    public async Task<IReadOnlyList<WslUsage>> MeasureAsync(string distro, CancellationToken cancellationToken = default)
    {
        var (exit, output, error) = await RunAsync(["-d", distro, "--", "bash", "-lc", MeasureScript], cancellationToken).ConfigureAwait(false);
        if (exit != 0)
        {
            // Never report "nothing to clean" when the truth is "could not look".
            throw new InvalidOperationException($"wsl -d {distro} exited with {exit}: {error.Trim()}");
        }

        var usage = new List<WslUsage>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Trim().Split('|');
            if (parts.Length != 3 || !long.TryParse(parts[1], out var bytes) || !long.TryParse(parts[2], out var files))
            {
                continue;
            }

            usage.Add(new WslUsage(parts[0], Math.Max(0, bytes), Math.Max(0, files)));
        }

        return usage;
    }

    public async Task<int> CleanAsync(string distro, string recipe, bool dryRun, CancellationToken cancellationToken = default)
    {
        if (dryRun)
        {
            return 0;
        }

        var command = WslRecipes.Resolve(recipe);
        if (command is null)
        {
            return -1;
        }

        var (exit, _, _) = await RunAsync(["-d", distro, "--", "bash", "-lc", command], cancellationToken).ConfigureAwait(false);
        return exit;
    }

    private static async Task<(int Exit, string Output, string Error)> RunAsync(string[] arguments, CancellationToken cancellationToken)
    {
        var info = new ProcessStartInfo
        {
            FileName = "wsl.exe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = null,
        };

        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = info };
        if (!process.Start())
        {
            return (-1, string.Empty, "wsl.exe could not be started.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ScanTimeout);

        try
        {
            var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            return (process.ExitCode, Decode(await stdout.ConfigureAwait(false)), Decode(await stderr.ConfigureAwait(false)));
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception)
            {
                // Already gone.
            }

            throw;
        }
    }

    /// <summary>wsl.exe emits UTF-16LE when redirected; bash emits UTF-8. Normalise both.</summary>
    private static string Decode(string text)
    {
        if (text.Length == 0)
        {
            return text;
        }

        if (text.IndexOf('\0') >= 0)
        {
            return text.Replace("\0", string.Empty, StringComparison.Ordinal).TrimEnd();
        }

        return text.TrimEnd();
    }
}
