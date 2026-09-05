using System.Windows.Controls;
using SteadySight.Models;
using SteadySight.Services;

namespace SteadySight.Views;

public partial class HorizonView : ConfigViewBase
{
    private bool _syncing;

    public HorizonView(ConfigStore store) : base(store)
    {
        InitializeComponent();
        StyleCombo.SelectionChanged += StyleCombo_Changed;
        OpacitySlider.ValueChanged += OpacitySlider_Changed;
        OffsetSlider.ValueChanged += OffsetSlider_Changed;
        OpacityValue.Tag = "%";
        OffsetValue.Tag = "%";
    }

    protected override void RefreshFromConfig()
    {
        _syncing = true;
        try
        {
            var c = Store.State;
            SetCombo(StyleCombo, c.HorizonStyle.ToString());
            SetSlider(OpacitySlider, c.HorizonOpacity * 100);
            OpacityValue.Text = $"{c.HorizonOpacity * 100:0}%";
            SetSlider(OffsetSlider, c.HorizonOffsetPercent);
            OffsetValue.Text = $"{c.HorizonOffsetPercent:0}%";
        }
        finally { _syncing = false; }
    }

    private void StyleCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        if (StyleCombo.SelectedItem is ComboBoxItem item && item.Tag != null)
            Store.Mutate(c => c.HorizonStyle = ParseEnum<HorizonStyle>(item.Tag));
    }

    private void OpacitySlider_Changed(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing || OpacityValue == null) return;
        OpacityValue.Text = $"{e.NewValue:0}%";
        Store.Mutate(c => c.HorizonOpacity = e.NewValue / 100.0);
    }

    private void OffsetSlider_Changed(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing || OffsetValue == null) return;
        OffsetValue.Text = $"{e.NewValue:0}%";
        Store.Mutate(c => c.HorizonOffsetPercent = e.NewValue);
    }
}
