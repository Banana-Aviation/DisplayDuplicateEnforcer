using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DisplayDuplicateEnforcer;

public static partial class DuplicateEnforcer
{
    private static int _displayCount = -1;


    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int SetDisplayConfig(
        uint numPathArrayElements,
        IntPtr pathArray,
        uint numModeArrayElements,
        IntPtr modeArray,
        uint flags);

    private const uint SdcApply = 0x00000080;
    private const uint SdcTopologyClone = 0x00000002;


    public static void ReactToDisplayCountChange()
    {
        if (!TrayApp.ShouldEnforce) return;
        _displayCount = Screen.AllScreens.Length;
        ReactToDisplayCountChange(_displayCount);
    }

    private static void ReactToDisplayCountChange(int displayCount)
    {
        try
        {
            Logger.Log($"Display Count Changed: {displayCount}");
            Logger.Log($"TrayApp.RequiredScaling={TrayApp.RequiredScaling}");
            for (var i = 0; i < _displayCount; i++)
            {
                var processStartInfo = new ProcessStartInfo()
                {
                    FileName = "SetDpi.exe",
                    Arguments = $"{TrayApp.RequiredScaling} {i+1}",
                    CreateNoWindow = true,
                    LoadUserProfile = false,
                    UseShellExecute = false,
                };
                var process = Process.Start(processStartInfo);
                process?.WaitForExit();
            }
            if (displayCount != 2) return;

            var result = SetDisplayConfig(
                0, IntPtr.Zero,
                0, IntPtr.Zero,
                SdcApply | SdcTopologyClone);
            if (result != 0)
            {
                Logger.Log($"Error: result={result}; Display Count: {displayCount}");
            }
        }
        catch (Exception e)
        {
            Logger.Log($"Error: {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }
}