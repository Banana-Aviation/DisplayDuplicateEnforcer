using System.Reflection;
using System.Drawing;
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
    private MessageWindow messageWindow;
    public TrayApp()
    {
        try
        {
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
            DuplicateEnforcer.Log($"Error: {e.Message}\nStackTrace: {e.StackTrace}");
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
}