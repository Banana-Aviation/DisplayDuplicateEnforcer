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
            if (displayCount != 2) return;
            Logger.Log($"TrayApp.RequiredScaling={TrayApp.RequiredScaling}");
            // insert call to SetDpi.exe
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