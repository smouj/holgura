using System.Runtime.InteropServices;
using Holgura.Contracts;

namespace Holgura.Platform.Windows;

/// <summary>
/// Shell API wrapper. The bin is a shell concept, not a folder, so it can only be measured and
/// emptied through SHQueryRecycleBin / SHEmptyRecycleBin.
/// </summary>
public sealed class RecycleBinService : IRecycleBin
{
    private const uint SherbNoConfirmation = 0x00000001;
    private const uint SherbNoProgressUi = 0x00000002;
    private const uint SherbNoSound = 0x00000004;

    private const int SOk = 0;

    public RecycleBinStats Query()
    {
        var info = new ShQueryRbInfo
        {
            CbSize = Marshal.SizeOf<ShQueryRbInfo>(),
        };

        var hr = SHQueryRecycleBin(null, ref info);
        return hr != SOk ? new RecycleBinStats(0, 0) : new RecycleBinStats(info.NumItems, info.NumBytes);
    }

    public bool Empty()
    {
        var flags = SherbNoConfirmation | SherbNoProgressUi | SherbNoSound;
        return SHEmptyRecycleBin(IntPtr.Zero, null, flags) == SOk;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShQueryRbInfo
    {
        public int CbSize;
        public long NumItems;
        public long NumBytes;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string? pszRootPath, ref ShQueryRbInfo pShQueryRbInfo);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);
}
