using System.Windows.Threading;
using SteadySight.Models;

namespace SteadySight.Services;

public sealed class BreakReminder
{
    private readonly ConfigStore _store;
    private readonly OverlayManager _overlay;
    private readonly DispatcherTimer _timer;
    private DateTime _lastBreak;
    private BreakInterval _interval;

    public BreakReminder(ConfigStore store, OverlayManager overlay)
    {
        _store = store;
        _overlay = overlay;
        _timer = new DispatcherTimer();
        _timer.Tick += (_, _) => Fire();
        _store.Changed += OnConfigChanged;
    }

    public void Start()
    {
        _lastBreak = DateTime.Now;
        _interval = _store.State.BreakEvery;
        ReloadInterval();
    }

    public void Stop() => _timer.Stop();

    private void OnConfigChanged()
    {
        var interval = _store.State.BreakEvery;
        if (interval == _interval) return;
        _interval = interval;
        _lastBreak = DateTime.Now;
        ReloadInterval();
    }

    private void ReloadInterval()
    {
        _timer.Stop();
        var minutes = _store.State.BreakEvery switch
        {
            BreakInterval.Every15 => 15,
            BreakInterval.Every30 => 30,
            BreakInterval.Every45 => 45,
            BreakInterval.Every60 => 60,
            _ => 0
        };
        if (minutes <= 0) return;
        var elapsed = DateTime.Now - _lastBreak;
        var remaining = TimeSpan.FromMinutes(minutes) - elapsed;
        _timer.Interval = remaining > TimeSpan.Zero ? remaining : TimeSpan.FromMinutes(minutes);
        _timer.Start();
    }

    private void Fire()
    {
        _lastBreak = DateTime.Now;
        _overlay.ShowBreakNotice(25);
        ReloadInterval();
    }
}
