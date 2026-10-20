using System.Text.Json;

namespace Holgura.Core;

/// <summary>Append-only JSON lines file; one operation per line.</summary>
public sealed class JsonHistoryStore : IHistoryStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonHistoryStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Holgura",
            "history.jsonl");
    }

    public string FilePath => _filePath;

    public async Task AppendAsync(HistoryEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var line = JsonSerializer.Serialize(entry, Options);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(_filePath, line + Environment.NewLine, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<HistoryEntry>> LoadAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var entries = new List<HistoryEntry>();
            foreach (var line in await File.ReadAllLinesAsync(_filePath, cancellationToken).ConfigureAwait(false))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    var parsed = JsonSerializer.Deserialize<HistoryEntry>(line, Options);
                    if (parsed is not null)
                    {
                        entries.Add(parsed);
                    }
                }
                catch (JsonException)
                {
                    // A corrupt line must not hide the rest of the history.
                }
            }

            entries.Reverse();
            return entries.Take(limit).ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }
}
