using System.Windows;
using SteadySight.Models;
using SteadySight.Services;

namespace SteadySight.Views;

public partial class PresetsView : ConfigViewBase
{
    public PresetsView(ConfigStore store) : base(store)
    {
        InitializeComponent();
    }

    protected override void RefreshFromConfig()
    {
        CurrentPreset.Text = "当前：" + Store.State.ProfileName;
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string key)
        {
            Store.Mutate(c => Presets.Apply(c, key));
        }
    }
}
