using Holgura.Contracts;
using Holgura.Engine.Modules;

namespace Holgura.Engine;

public static class ModuleRegistry
{
    public static IReadOnlyList<ICleanerModule> CreateDefault(IWslBridge? wsl = null, IRecycleBin? recycleBin = null)
    {
        var modules = new List<ICleanerModule>
        {
            new TempCleaner(),
            new WindowsTempCleaner(),
            new ThumbnailCleaner(),
            new WindowsErrorReportingCleaner(),
            new CrashDumpCleaner(),
            new BrowserCacheCleaner(),
        };

        if (recycleBin is not null)
        {
            modules.Add(new RecycleBinCleaner(recycleBin));
        }

        if (wsl is not null)
        {
            modules.Add(new WslCacheModule(wsl));
        }

        return modules;
    }
}
