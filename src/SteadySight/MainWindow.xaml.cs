using System.Windows;
using System.Windows.Controls;
using SteadySight.Views;

namespace SteadySight;

public partial class MainWindow : Window
{
    private readonly ConfigViewBase[] _pages;

    public MainWindow()
    {
        InitializeComponent();

        var store = App.Store;
        _pages = new ConfigViewBase[]
        {
            new OverviewView(store),
            new EdgeView(store),
            new AnchorView(store),
            new DotsView(store),
            new HorizonView(store),
            new RuntimeView(store),
            new PresetsView(store)
        };

        NavList.SelectedIndex = 0;
    }

    private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var idx = NavList.SelectedIndex;
        if (idx < 0 || idx >= _pages.Length) return;
        if (!ReferenceEquals(PageHost.Content, _pages[idx]))
            PageHost.Content = _pages[idx];

        var (title, desc) = idx switch
        {
            0 => ("总览", "快速了解当前状态与快捷方式。"),
            1 => ("边缘稳定", "屏幕四周的柔和参照，减少周边视觉刺激。"),
            2 => ("中心锚点", "屏幕中心的固定参考点，提供静止参照。"),
            3 => ("动态圆点", "随视角移动反向漂移的人造光流点。"),
            4 => ("水平参考线", "一条水平参考线，帮助维持空间定向。"),
            5 => ("运行设置", "叠加目标、刷新频率与健康提醒。"),
            _ => ("预设与依据", "按游戏类型切换整套配置，回顾设计依据。")
        };
        PageTitle.Text = title;
        PageDesc.Text = desc;
    }
}
