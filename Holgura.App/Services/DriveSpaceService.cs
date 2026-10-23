using Holgura.Core;
using System.IO;

namespace Holgura.App.Services;

public sealed record DriveSpaceSnapshot(string Name, string VolumeLabel, long TotalBytes, long AvailableBytes)
{
    public string Label => string.IsNullOrWhiteSpace(VolumeLabel) ? Name : $"{Name}  {VolumeLabel}";

    public string FreeText => SizeFormatter.Format(AvailableBytes);

    public string CapacityText => $"{SizeFormatter.Format(TotalBytes)} total";

    public string UsageText => $"{UsedPercent:0}% utilizado";

    public double UsedPercent => TotalBytes <= 0
        ? 0
        : Math.Clamp((TotalBytes - AvailableBytes) * 100d / TotalBytes, 0, 100);
}

public sealed class DriveSpaceService
{
    public IReadOnlyList<DriveSpaceSnapshot> ReadReadyFixedDrives()
    {
        var drives = new List<DriveSpaceSnapshot>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.DriveType != DriveType.Fixed || !drive.IsReady || drive.TotalSize <= 0)
                {
                    continue;
                }

                drives.Add(new DriveSpaceSnapshot(
                    drive.Name.TrimEnd('\\'),
                    drive.VolumeLabel,
                    drive.TotalSize,
                    Math.Clamp(drive.AvailableFreeSpace, 0, drive.TotalSize)));
            }
            catch (IOException)
            {
                // A drive can disconnect between enumeration and reading its properties.
            }
            catch (UnauthorizedAccessException)
            {
                // Keep reporting the other drives if one volume denies its metadata.
            }
        }

        return drives.OrderBy(drive => drive.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
