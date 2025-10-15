using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Microsoft.Win32;

namespace DisplayDuplicateEnforcer;

public class SetDpi
{
    public static void SetDpiOnDisplay(int dpiToSet, int displayIndex)
    {
        var displayDataCache = GetDisplayData();

        displayIndex -= 1;

        if (!DpiFound(dpiToSet))
        {
            Logger.Log($"Invalid DPI scale value: {dpiToSet}");
            return;
        }

        var success = DpiHelper.SetDpiScaling(displayDataCache[displayIndex].m_adapterId,
            displayDataCache[displayIndex].m_sourceID, dpiToSet);
        if (!success)
        {
            Logger.Log("SetDPIScaling failed miserably");
            return;
        }

        if (displayIndex != 0) return;
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop\WindowMetrics");
            var value = (int)(dpiToSet * 0.96);
            key.SetValue("AppliedDPI", value, RegistryValueKind.DWord);
        }
        catch (Exception e)
        {
            Logger.Log(e.Message);
        }
    }

    private static unsafe List<DisplayData> GetDisplayData()
    {
        var pathsV = new List<DISPLAYCONFIG_PATH_INFO>();
        var modesV = new List<DISPLAYCONFIG_MODE_INFO>();
        const QUERY_DISPLAY_CONFIG_FLAGS flags = QUERY_DISPLAY_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS;
        if (!DpiHelper.GetPathsAndModes(pathsV, modesV, flags))
        {
            Logger.Log("GetPathsAndModes failed miserably");
        }

        var idx = 0;
        var dataCache = Enumerable.Repeat(new DisplayData(), pathsV.Count).ToList();


        for (var i = 0; i < pathsV.Count; i++)
        {
            var adapterLuid = pathsV[i].targetInfo.adapterId;
            var targetId = pathsV[i].targetInfo.id;
            var sourceId = pathsV[i].sourceInfo.id;
            DISPLAYCONFIG_TARGET_DEVICE_NAME deviceName = default;
            deviceName.header.size =
                (uint)Marshal
                    .SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>();
            deviceName.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
            deviceName.header.adapterId = adapterLuid;
            deviceName.header.id = targetId;

            if ((int)WIN32_ERROR.ERROR_SUCCESS != PInvoke.DisplayConfigGetDeviceInfo(&deviceName.header))
            {
                Logger.Log("DisplayConfigGetDeviceInfo failed miserably");
            }
            else
            {
                var dd = new DisplayData
                {
                    m_adapterId = adapterLuid,
                    m_sourceID = (int)sourceId,
                    m_targetID = (int)targetId
                };
                dataCache[idx] = dd;
            }

            idx++;
        }

        return dataCache;
    }


    private static bool DpiFound(int val)
    {
        return DpiHelper.DpiVals.Contains((uint)val);
    }
}