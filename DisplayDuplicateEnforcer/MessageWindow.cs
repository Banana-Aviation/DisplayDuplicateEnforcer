namespace DisplayDuplicateEnforcer;

public sealed class MessageWindow : NativeWindow, IDisposable
{
    private const int WM_DEVICECHANGE = 0x0219;
    private const int DBT_DEVNODES_CHANGED = 0x0007;
    private const int WM_DISPLAYCHANGE = 0x007E;

    public MessageWindow()
    {
        CreateHandle(new CreateParams());
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_DISPLAYCHANGE || (m.Msg == WM_DEVICECHANGE && m.WParam.ToInt32() == DBT_DEVNODES_CHANGED))
        {
            DuplicateEnforcer.ReactToDisplayCountChange();
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        DestroyHandle();
    }
}