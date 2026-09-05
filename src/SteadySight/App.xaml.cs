using System.IO;
using System.Windows;
using System.Windows.Threading;
using SteadySight.Models;
using SteadySight.Services;

namespace SteadySight;

public partial class App : Application
{
    private static readonly string ConfigPath =
        Environment.GetEnvironmentVariable("STEADYSIGHT_CONFIG") is { Length: > 0 } overridePath
            ? overridePath
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SteadySight", "settings.json");

    public static ConfigStore Store { get; private set; } = null!;
    public static OverlayManager Overlay { get; private set; } = null!;
    public static HotkeyManager Hotkeys { get; private set; } = null!;
    public static BreakReminder Breaks { get; private set; } = null!;

    private TrayService? _tray;
    private SingleInstanceGuard? _instanceGuard;
    private bool _exitRequested;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _instanceGuard = SingleInstanceGuard.TryAcquire("Local\\SteadySight.App.Instance");
        if (_instanceGuard == null)
        {
            Shutdown();
            return;
        }

        var reset = e.Args.Any(a => string.Equals(a, "--reset", StringComparison.OrdinalIgnoreCase));
        var config = reset ? new SteadyConfig() : ConfigStore.Load(ConfigPath);
        Store = new ConfigStore(config, ConfigPath);
        if (reset) Store.Save();
        Overlay = new OverlayManager(Store);
        Hotkeys = new HotkeyManager((Window?)null, Store, ToggleFeature);
        Breaks = new BreakReminder(Store, Overlay);

        _tray = new TrayService(Store, ShowMain, ToggleFeature, ShutdownApp);
        Hotkeys.Start();
        Breaks.Start();
        Overlay.Start();

        ShowMain();
    }

    private void ShowMain()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(ShowMain);
            return;
        }

        if (MainWindow is MainWindow w)
        {
            if (w.IsVisible)
            {
                w.Activate();
                return;
            }
            w.Show();
            w.Activate();
            return;
        }

        w = new MainWindow();
        MainWindow = w;
        w.Closing += (_, e) =>
        {
            if (!_exitRequested)
            {
                e.Cancel = true;
                w.Hide();
            }
        };
        w.Show();
        w.Activate();
    }

    private void ToggleFeature(string key)
    {
        Dispatcher.Invoke(() => Hotkeys.HandleToggle(key, Overlay));
    }

    private void ShutdownApp()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(ShutdownApp);
            return;
        }

        _exitRequested = true;
        MainWindow?.Close();

        Hotkeys.Stop();
        Breaks.Stop();
        Overlay.Stop();
        _tray?.Dispose();
        Store.Save();
        _instanceGuard?.Dispose();
        _instanceGuard = null;
        Shutdown();
    }
}
