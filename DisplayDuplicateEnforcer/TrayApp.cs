using System.Reflection;
using Microsoft.Win32;

namespace DisplayDuplicateEnforcer;

public class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private ContextMenuStrip _contextMenu;
    private ToolStripMenuItem _disabledMenuItem;
    private ToolStripMenuItem _enabledMenuItem;
    private bool _isEnabled = true;
#pragma warning disable CA2211
    public static bool ShouldEnforce = true;
    public static int RequiredScaling;
    private MessageWindow messageWindow;

    public TrayApp()
    {
        try
        {
            RequiredScaling = GetRequiredScaling();
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("DisplayDuplicateEnforcer.Resources.AppIcon.ico");
            InitializeContextMenu();
            _notifyIcon = new NotifyIcon
            {
                Icon = new Icon(stream),
                Visible = true,
                ContextMenuStrip = _contextMenu
            };
            _notifyIcon.MouseClick += NotifyIconOnMouseClick;
            Application.ApplicationExit += ApplicationOnApplicationExit;
            messageWindow = new MessageWindow();
            
            DuplicateEnforcer.ReactToDisplayCountChange();
        }
        catch (Exception e)
        {
            Logger.Log($"Error: {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }

    private void ApplicationOnApplicationExit(object? sender, EventArgs e)
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    private void NotifyIconOnMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right) return;
        var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
        mi?.Invoke(_notifyIcon, null);
    }


    private void InitializeContextMenu()
    {
        _contextMenu = new ContextMenuStrip();

        _disabledMenuItem = new ToolStripMenuItem
        {
            Text = "Disabled",
            Checked = !_isEnabled
        };
        _enabledMenuItem = new ToolStripMenuItem
        {
            Text = "Enabled",
            Checked = _isEnabled
        };
        _disabledMenuItem.Click += ToolStripMenuItemClick;
        _enabledMenuItem.Click += ToolStripMenuItemClick;
        _contextMenu.Items.Add(_disabledMenuItem);
        _contextMenu.Items.Add(_enabledMenuItem);
        _contextMenu.Closing += ContextMenuOnClosing;
    }

    private static void ContextMenuOnClosing(object? sender, ToolStripDropDownClosingEventArgs e)
    {
        if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
        {
            e.Cancel = true;
        }
    }

    private void ToolStripMenuItemClick(object? sender, EventArgs e)
    {
        _isEnabled = !_isEnabled;
        _disabledMenuItem.Checked = !_isEnabled;
        _enabledMenuItem.Checked = _isEnabled;
        ShouldEnforce = _isEnabled;
        if (_isEnabled)
        {
            DuplicateEnforcer.ReactToDisplayCountChange();
        }
    }

    private static int GetRequiredScaling()
    {
        const int defaultValue = 100;
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\DDE");
        if (key is null)
        {
            return defaultValue;
        }

        var obj = key.GetValue("BaseScaling", 0);
        var dword = (int)obj;
        if (dword == 0)
        {
            return defaultValue;
        }
        key.Close();
        return dword;
    }
}