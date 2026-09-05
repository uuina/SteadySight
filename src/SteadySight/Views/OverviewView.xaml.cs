using System.Windows;
using SteadySight.Models;
using SteadySight.Services;

namespace SteadySight.Views;

public partial class OverviewView : ConfigViewBase
{
    public OverviewView(ConfigStore store) : base(store)
    {
        InitializeComponent();
        ToggleBtn.Click += (_, _) =>
        {
            Store.Mutate(c => c.OverlayEnabled = !c.OverlayEnabled);
            RefreshFromConfig();
        };
    }

    protected override void RefreshFromConfig()
    {
        var c = Store.State;
        var on = c.OverlayEnabled;
        ToggleBtn.Content = on ? "关闭叠加层" : "开启叠加层";
        StatusText.Text = on ? "运行中" : "已暂停";
        StatusText.Foreground = (System.Windows.Media.Brush)(on
            ? FindResource("App.Success")
            : FindResource("App.Danger"));
        PresetText.Text = "预设：" + c.ProfileName;
        TargetText.Text = "目标：" + (c.Target switch
        {
            OverlayTarget.AllMonitors => "所有显示器",
            OverlayTarget.Primary => "主显示器",
            _ => "跟随前台窗口"
        });
    }
}
