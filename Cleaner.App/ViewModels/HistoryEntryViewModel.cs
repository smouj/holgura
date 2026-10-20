using Cleaner.Core;

namespace Cleaner.App.ViewModels;

public sealed class HistoryEntryViewModel(HistoryEntry entry)
{
    public string When => entry.TimestampUtc.ToLocalTime().ToString("dd/MM/yyyy · HH:mm");

    public string Summary => $"{SizeFormatter.Format(entry.BytesFreed)} liberados · {entry.FilesDeleted:N0} archivos · {entry.FilesSkipped:N0} omitidos · {entry.Errors:N0} errores";

    public string Kind => entry.DryRun ? "Simulación" : "Limpieza";

    public string Duration => $"{entry.Duration.TotalSeconds:0.0} s";

    public string Details => string.Join("\n", entry.Modules.Select(m => $"{m.Title}: {SizeFormatter.Format(m.BytesFreed)} ({m.Deleted:N0} archivos)"));
}
