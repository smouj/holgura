using Cleaner.Contracts;
using Cleaner.Engine.Modules;

namespace Cleaner.Engine;

public static class ModuleRegistry
{
    public static IReadOnlyList<ICleanerModule> CreateDefault(IWslBridge? wsl = null)
    {
        var modules = new List<ICleanerModule>
        {
            new TempCleaner(),
            new WindowsTempCleaner(),
            new ThumbnailCleaner(),
        };

        if (wsl is not null)
        {
            modules.Add(new WslCacheModule(wsl));
        }

        return modules;
    }
}
