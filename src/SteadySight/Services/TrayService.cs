using System.Drawing;
using System.Windows.Forms;

namespace SteadySight.Services;

public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ConfigStore _store;
    private readonly Action _showMain;
    private readonly Action<string> _toggle;
    private readonly Action _exit;

    public TrayService(ConfigStore store, Action showMain, Action<string> toggle, Action exit)
    {
        _store = store;
        _showMain = showMain;
        _toggle = toggle;
        _exit = exit;

        System.Drawing.Icon appIcon = SystemIcons.Application;
        try
        {
            if (!string.IsNullOrEmpty(Environment.ProcessPath))
            {
                var extracted = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath);
                if (extracted != null) appIcon = extracted;
            }
        }
        catch { }

        _icon = new NotifyIcon
        {
            Text = "SteadySight 稳视",
            Icon = appIcon,
            Visible = true
        };
        var menu = new ContextMenuStrip();
        menu.Items.Add("显示主界面", null, (_, _) => _showMain());
        menu.Items.Add("开关叠加层", null, (_, _) => _toggle("overlay"));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => _exit());
        _icon.ContextMenuStrip = menu;
        _icon.DoubleClick += (_, _) => _showMain();
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
