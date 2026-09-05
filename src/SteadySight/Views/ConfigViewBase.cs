using System.Windows.Controls;
using SteadySight.Models;
using SteadySight.Services;

namespace SteadySight.Views;

/// <summary>配置视图基类：统一处理配置变更刷新。</summary>
public abstract class ConfigViewBase : UserControl
{
    protected ConfigStore Store { get; }

    protected ConfigViewBase(ConfigStore store)
    {
        Store = store;
        Loaded += (_, _) => RefreshFromConfig();
        store.Changed += RefreshFromConfig;
    }

    protected virtual void RefreshFromConfig() { }

    protected static void SetCombo(ComboBox combo, object value)
    {
        var wanted = value?.ToString();
        var item = combo.Items.Cast<object>()
            .OfType<System.Windows.Controls.ComboBoxItem>()
            .FirstOrDefault(i => string.Equals(i.Tag?.ToString(), wanted, StringComparison.OrdinalIgnoreCase));
        if (item != null)
        {
            if (!ReferenceEquals(combo.SelectedItem, item)) combo.SelectedItem = item;
            return;
        }
        combo.SelectedValue = value;
    }

    protected static void SetSlider(Slider slider, double value)
    {
        if (Math.Abs(slider.Value - value) > 0.001) slider.Value = value;
    }

    protected static void SetSliderLabel(TextBlock label, double value) =>
        label.Text = $"{value:0.#}{GetUnit(label.Tag?.ToString() ?? "%")}";

    private static string GetUnit(string kind) => kind == "px" ? " px" :
        kind == "s" ? " 秒" : "%";

    protected static TEnum ParseEnum<TEnum>(object value) where TEnum : struct =>
        Enum.TryParse<TEnum>(value?.ToString(), out var r) ? r : default;
}
