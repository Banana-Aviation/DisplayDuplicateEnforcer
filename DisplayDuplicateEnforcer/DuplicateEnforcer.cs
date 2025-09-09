using System.Runtime.InteropServices;

namespace DisplayDuplicateEnforcer;

public static partial class DuplicateEnforcer
{
    private static int _displayCount = -1;
    private static readonly string LogFile = $@"{Environment.GetEnvironmentVariable("USERPROFILE")}\Documents\dde.log";

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
            Log($"Display Count Changed: {displayCount}");
            if (displayCount != 2) return;

            var result = SetDisplayConfig(
                0, IntPtr.Zero,
                0, IntPtr.Zero,
                SdcApply | SdcTopologyClone);
            if (result != 0)
            {
                Log($"Error: result={result}; Display Count: {displayCount}");
            }
        }
        catch (Exception e)
        {
            Log($"Error: {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }

    public static void Log(string s)
    {
        try
        {
            File.AppendAllText(LogFile, $"{s}{Environment.NewLine}");
        }
        catch (Exception e)
        {
            // ignored
        }
    }
    
}