using System.Windows.Controls;
using System.Windows.Media;
using SteadySight.Models;
using SteadySight.Services;

namespace SteadySight.Views;

public partial class AnchorView : ConfigViewBase
{
    private bool _syncing;

    public AnchorView(ConfigStore store) : base(store)
    {
        InitializeComponent();
        StyleCombo.SelectionChanged += StyleCombo_Changed;
        SizeSlider.ValueChanged += SizeSlider_Changed;
        OpacitySlider.ValueChanged += OpacitySlider_Changed;
        ColorBox.TextChanged += ColorBox_TextChanged;
        ResetColor.Click += ResetColor_Click;
        SizeValue.Tag = "px";
        OpacityValue.Tag = "%";
    }

    protected override void RefreshFromConfig()
    {
        _syncing = true;
        try
        {
            var c = Store.State;
            SetCombo(StyleCombo, c.AnchorStyle.ToString());
            SetSlider(SizeSlider, c.AnchorSize);
            SizeValue.Text = $"{c.AnchorSize:0} px";
            SetSlider(OpacitySlider, c.AnchorOpacity * 100);
            OpacityValue.Text = $"{c.AnchorOpacity * 100:0}%";
            if (!ColorBox.IsFocused && ColorBox.Text != c.AnchorColor)
                ColorBox.Text = c.AnchorColor;
        }
        finally { _syncing = false; }
    }

    private void StyleCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        if (StyleCombo.SelectedItem is ComboBoxItem item && item.Tag != null)
            Store.Mutate(c => c.AnchorStyle = ParseEnum<AnchorStyle>(item.Tag));
    }

    private void SizeSlider_Changed(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing || SizeValue == null) return;
        SizeValue.Text = $"{e.NewValue:0} px";
        Store.Mutate(c => c.AnchorSize = e.NewValue);
    }

    private void OpacitySlider_Changed(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing || OpacityValue == null) return;
        OpacityValue.Text = $"{e.NewValue:0}%";
        Store.Mutate(c => c.AnchorOpacity = e.NewValue / 100.0);
    }

    private void ColorBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_syncing || ColorBox == null) return;
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(ColorBox.Text.Trim());
            if (color != default)
                Store.Mutate(c => c.AnchorColor = ColorBox.Text.Trim());
        }
        catch
        {
            // 非法值不写入
        }
    }

    private void ResetColor_Click(object sender, System.Windows.RoutedEventArgs e) =>
        Store.Mutate(c => c.AnchorColor = "#7CF5C4");
}
