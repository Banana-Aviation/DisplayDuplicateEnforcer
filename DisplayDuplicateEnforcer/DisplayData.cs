using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace DisplayDuplicateEnforcer;

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayData
{
    public LUID m_adapterId;
    public int m_targetID;
    public int m_sourceID;

    public DisplayData()
    {
        m_adapterId = new LUID();
        m_targetID = m_sourceID = -1;
    }
}