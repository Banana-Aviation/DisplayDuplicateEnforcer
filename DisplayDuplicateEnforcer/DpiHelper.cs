using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;

namespace DisplayDuplicateEnforcer;

public class DpiHelper
{
    public static readonly uint[] DpiVals = [100, 125, 150, 175, 200, 225, 250, 300, 350, 400, 450, 500];
     internal static unsafe bool GetPathsAndModes(List<DISPLAYCONFIG_PATH_INFO> pathsV,
        List<DISPLAYCONFIG_MODE_INFO> modesV, QUERY_DISPLAY_CONFIG_FLAGS flags)
    {
        var status = PInvoke.GetDisplayConfigBufferSizes(flags, out var numPaths, out var numModes);
        if (status != WIN32_ERROR.ERROR_SUCCESS)
        {
            return false;
        }

        var paths = stackalloc DISPLAYCONFIG_PATH_INFO[(int)numPaths];
        var modes = stackalloc DISPLAYCONFIG_MODE_INFO[(int)numModes];

        status = PInvoke.QueryDisplayConfig(flags, &numPaths, paths, &numModes, modes, null);
        if (status != WIN32_ERROR.ERROR_SUCCESS)
        {
            return false;
        }

        for (var i = 0; i < numPaths; i++)
        {
            pathsV.Add(paths[i]);
        }

        for (var i = 0; i < numModes; i++)
        {
            modesV.Add(modes[i]);
        }

        return true;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DpiScalingInfo
    {
        public uint minimum = 100;
        public uint maximum = 100;
        public uint current = 100;
        public uint recommended = 100;
        public bool bInitDone = false;

        public DpiScalingInfo()
        {
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigSourceDpiScaleSet
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;

        public int scaleRel;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigSourceDpiScaleGet
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public int minScaleRel;
        public int curScaleRel;
        public int maxScaleRel;
    }

    private enum DisplayConfigDeviceInfoTypeCustom
    {
        DisplayConfigDeviceInfoGetDpiScale = -3,
        DisplayConfigDeviceInfoSetDpiScale = -4
    }

    internal static DpiScalingInfo GetDpiScalingInfo(LUID adapterId, int sourceId)
    {
        var dpiInfo = new DpiScalingInfo();
        var requestPacket = new DisplayConfigSourceDpiScaleGet();
        requestPacket.header.type =
            (DISPLAYCONFIG_DEVICE_INFO_TYPE)DisplayConfigDeviceInfoTypeCustom
                .DisplayConfigDeviceInfoGetDpiScale;
        requestPacket.header.size = (uint)Marshal.SizeOf<DisplayConfigSourceDpiScaleGet>();
        if (0x20 != Marshal.SizeOf<DisplayConfigSourceDpiScaleGet>())
        {
            throw new Exception(
                " 0x20 != sizeof(DISPLAYCONFIG_SOURCE_DPI_SCALE_GET) if this fails => OS has changed somthing, and our reverse enginnering knowledge about the API is outdated");
        }

        requestPacket.header.adapterId = adapterId;
        requestPacket.header.id = (uint)sourceId;
        var res = PInvoke.DisplayConfigGetDeviceInfo(ref requestPacket.header);
        if ((int)WIN32_ERROR.ERROR_SUCCESS != res) return dpiInfo;
        if (requestPacket.curScaleRel < requestPacket.minScaleRel)
        {
            requestPacket.curScaleRel = requestPacket.minScaleRel;
        }
        else if (requestPacket.curScaleRel > requestPacket.maxScaleRel)
        {
            requestPacket.curScaleRel = requestPacket.maxScaleRel;
        }

        var minAbs = Math.Abs(requestPacket.minScaleRel);
        if (DpiVals.Length < minAbs + requestPacket.maxScaleRel + 1) return dpiInfo;
        dpiInfo.current = DpiVals[minAbs + requestPacket.curScaleRel];
        dpiInfo.recommended = DpiVals[minAbs];
        dpiInfo.maximum = DpiVals[minAbs + requestPacket.maxScaleRel];
        dpiInfo.bInitDone = true;

        return dpiInfo;
    }

    internal static bool SetDpiScaling(LUID adapterId, int sourceId, int dpiPercentToSet)
    {
        var dPiScalingInfo = GetDpiScalingInfo(adapterId, sourceId);
        if (dpiPercentToSet == dPiScalingInfo.current)
        {
            return true;
        }

        if (dpiPercentToSet < dPiScalingInfo.minimum)
        {
            dpiPercentToSet = (int)dPiScalingInfo.minimum;
        }
        else if (dpiPercentToSet > dPiScalingInfo.maximum)
        {
            dpiPercentToSet = (int)dPiScalingInfo.maximum;
        }

        int idx1 = -1, idx2 = -1;
        var i = 0;
        foreach (var val in DpiVals)
        {
            if (val == dpiPercentToSet)
            {
                idx1 = i;
            }

            if (val == dPiScalingInfo.recommended)
            {
                idx2 = i;
            }

            i++;
        }

        if (idx1 == -1 || idx2 == -1)
        {
            return false;
        }

        var dpiRelativeVal = idx1 - idx2;
        var setPacket = new DisplayConfigSourceDpiScaleSet();
        setPacket.header.adapterId = adapterId;
        setPacket.header.id = (uint)sourceId;
        setPacket.header.size = (uint)Marshal.SizeOf<DisplayConfigSourceDpiScaleSet>();
        if (0x18 != Marshal.SizeOf<DisplayConfigSourceDpiScaleSet>())
        {
            throw new Exception(
                "0x18 != Marshal.SizeOf<DisplayconfigSourceDpiScaleSet>() if this fails => OS has changed somthing, and our reverse enginnering knowledge about the API is outdated");
        }

        setPacket.header.type =
            (DISPLAYCONFIG_DEVICE_INFO_TYPE)DisplayConfigDeviceInfoTypeCustom
                .DisplayConfigDeviceInfoSetDpiScale;
        setPacket.scaleRel = dpiRelativeVal;
        var res = PInvoke.DisplayConfigSetDeviceInfo(setPacket.header);
        return (int)WIN32_ERROR.ERROR_SUCCESS == res;
    }
}