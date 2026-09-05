using System.Windows.Controls;
using SteadySight.Models;
using SteadySight.Services;

namespace SteadySight.Views;

public partial class EdgeView : ConfigViewBase
{
    private bool _syncing;

    public EdgeView(ConfigStore store) : base(store)
    {
        InitializeComponent();
        StyleCombo.SelectionChanged += StyleCombo_Changed;
        WidthSlider.ValueChanged += WidthSlider_Changed;
        OpacitySlider.ValueChanged += OpacitySlider_Changed;
        WidthValue.Tag = "%";
        OpacityValue.Tag = "%";
    }

    protected override void RefreshFromConfig()
    {
        _syncing = true;
        try
        {
            var c = Store.State;
            SetCombo(StyleCombo, c.EdgeStyle.ToString());
            SetSlider(WidthSlider, c.EdgeWidthPercent);
            WidthValue.Text = $"{c.EdgeWidthPercent:0}%";
            SetSlider(OpacitySlider, c.EdgeOpacity * 100);
            OpacityValue.Text = $"{c.EdgeOpacity * 100:0}%";
        }
        finally { _syncing = false; }
    }

    private void StyleCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        if (StyleCombo.SelectedItem is ComboBoxItem item && item.Tag != null)
            Store.Mutate(c => c.EdgeStyle = ParseEnum<EdgeStyle>(item.Tag));
    }

    private void WidthSlider_Changed(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing || WidthValue == null) return;
        WidthValue.Text = $"{e.NewValue:0}%";
        Store.Mutate(c => c.EdgeWidthPercent = e.NewValue);
    }

    private void OpacitySlider_Changed(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing || OpacityValue == null) return;
        OpacityValue.Text = $"{e.NewValue:0}%";
        Store.Mutate(c => c.EdgeOpacity = e.NewValue / 100.0);
    }
}
