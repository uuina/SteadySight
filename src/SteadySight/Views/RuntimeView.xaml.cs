using System.Windows.Controls;
using SteadySight.Models;
using SteadySight.Services;

namespace SteadySight.Views;

public partial class RuntimeView : ConfigViewBase
{
    private bool _syncing;

    public RuntimeView(ConfigStore store) : base(store)
    {
        InitializeComponent();
        TargetCombo.SelectionChanged += TargetCombo_Changed;
        FpsCombo.SelectionChanged += FpsCombo_Changed;
        BreakCombo.SelectionChanged += BreakCombo_Changed;
        HotkeyCheck.Checked += Hotkey_Changed;
        HotkeyCheck.Unchecked += Hotkey_Changed;
        TestBreak.Click += (_, _) => SteadySight.App.Overlay.ShowBreakNotice(15);
        ResetDefaults.Click += (_, _) =>
        {
            var fresh = new SteadyConfig();
            fresh.ProfileName = "标准";
            Store.ReplaceProfile(fresh);
        };
    }

    protected override void RefreshFromConfig()
    {
        _syncing = true;
        try
        {
            var c = Store.State;
            SetCombo(TargetCombo, c.Target.ToString());
            SetCombo(FpsCombo, c.FpsPreset.ToString());
            SetCombo(BreakCombo, c.BreakEvery.ToString());
            HotkeyCheck.IsChecked = c.GameHotkeysEnabled;
            var registered = SteadySight.App.Hotkeys?.RegisteredHotkeyCount ?? 0;
            HotkeyStatus.Text = !c.GameHotkeysEnabled
                ? "全局快捷键已关闭，不占用游戏按键。"
                : registered == 6
                    ? "6/6 个全局快捷键已注册。"
                    : $"仅注册 {registered}/6 个快捷键，部分按键可能被其他程序占用。";
            HotkeyStatus.Foreground = (System.Windows.Media.Brush)FindResource(
                c.GameHotkeysEnabled && registered < 6 ? "App.Danger" : "App.TextSecondary");
        }
        finally { _syncing = false; }
    }

    private void TargetCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        if (TargetCombo.SelectedItem is ComboBoxItem item && item.Tag != null)
            Store.Mutate(c => c.Target = ParseEnum<OverlayTarget>(item.Tag));
    }

    private void FpsCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        if (FpsCombo.SelectedItem is ComboBoxItem item && item.Tag != null)
            Store.Mutate(c => c.FpsPreset = ParseEnum<FrameRatePreset>(item.Tag));
    }

    private void BreakCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        if (BreakCombo.SelectedItem is ComboBoxItem item && item.Tag != null)
            Store.Mutate(c => c.BreakEvery = ParseEnum<BreakInterval>(item.Tag));
    }

    private void Hotkey_Changed(object sender, System.Windows.RoutedEventArgs e)
    {
        if (!_syncing) Store.Mutate(c => c.GameHotkeysEnabled = HotkeyCheck.IsChecked == true);
    }
}
