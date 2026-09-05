using System.Windows.Controls;
using SteadySight.Models;
using SteadySight.Services;

namespace SteadySight.Views;

public partial class DotsView : ConfigViewBase
{
    private bool _syncing;

    public DotsView(ConfigStore store) : base(store)
    {
        InitializeComponent();
        ModeCombo.SelectionChanged += ModeCombo_Changed;
        DensitySlider.ValueChanged += DensitySlider_Changed;
        SizeSlider.ValueChanged += SizeSlider_Changed;
        IntensitySlider.ValueChanged += IntensitySlider_Changed;
        ParallaxCheck.Checked += Parallax_Changed;
        ParallaxCheck.Unchecked += Parallax_Changed;
        FadeCheck.Checked += Fade_Changed;
        FadeCheck.Unchecked += Fade_Changed;
        DensityValue.Tag = "%";
        SizeValue.Tag = "px";
        IntensityValue.Tag = "%";
    }

    protected override void RefreshFromConfig()
    {
        _syncing = true;
        try
        {
            var c = Store.State;
            SetCombo(ModeCombo, c.DotMode.ToString());
            SetSlider(DensitySlider, c.DotDensity * 100);
            DensityValue.Text = $"{c.DotDensity * 100:0}%";
            SetSlider(SizeSlider, c.DotSize);
            SizeValue.Text = $"{c.DotSize:0.#} px";
            SetSlider(IntensitySlider, c.DotIntensity * 100);
            IntensityValue.Text = $"{c.DotIntensity * 100:0}%";
            ParallaxCheck.IsChecked = c.DotParallax;
            FadeCheck.IsChecked = c.DotFadeOnIdle;
        }
        finally { _syncing = false; }
    }

    private void ModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        if (ModeCombo.SelectedItem is ComboBoxItem item && item.Tag != null)
            Store.Mutate(c => c.DotMode = ParseEnum<DotMotionMode>(item.Tag));
    }

    private void DensitySlider_Changed(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing || DensityValue == null) return;
        DensityValue.Text = $"{e.NewValue:0}%";
        Store.Mutate(c => c.DotDensity = e.NewValue / 100.0);
    }

    private void SizeSlider_Changed(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing || SizeValue == null) return;
        SizeValue.Text = $"{e.NewValue:0.#} px";
        Store.Mutate(c => c.DotSize = e.NewValue);
    }

    private void IntensitySlider_Changed(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing || IntensityValue == null) return;
        IntensityValue.Text = $"{e.NewValue:0}%";
        Store.Mutate(c => c.DotIntensity = e.NewValue / 100.0);
    }

    private void Parallax_Changed(object sender, System.Windows.RoutedEventArgs e)
    {
        if (!_syncing) Store.Mutate(c => c.DotParallax = ParallaxCheck.IsChecked == true);
    }

    private void Fade_Changed(object sender, System.Windows.RoutedEventArgs e)
    {
        if (!_syncing) Store.Mutate(c => c.DotFadeOnIdle = FadeCheck.IsChecked == true);
    }
}
