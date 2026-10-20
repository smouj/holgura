using System.IO;
using System.Text.Json;
using Cleaner.Core;
using Cleaner.Platform.Windows;

namespace Cleaner.App.Services;

public static class ExclusionStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private static string FilePath => Path.Combine(WindowsPaths.AppDataDirectory, "exclusions.json");

    public static ExclusionService Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var rules = JsonSerializer.Deserialize<List<ExclusionRule>>(File.ReadAllText(FilePath), Options);
                if (rules is not null)
                {
                    return new ExclusionService(rules);
                }
            }
        }
        catch (Exception)
        {
            // A broken settings file must not stop the app from starting.
        }

        return new ExclusionService();
    }

    public static void Save(ExclusionService service)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(service.Rules, Options));
        }
        catch (Exception)
        {
            // Ignored on purpose: exclusions stay in memory for this session.
        }
    }
}
