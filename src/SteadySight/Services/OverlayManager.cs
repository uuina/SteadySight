using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using SteadySight.Models;
using SteadySight.Overlay;

namespace SteadySight.Services;

/// <summary>管理叠加窗口：目标显示器几何、活动窗口跟随、可见性切换。</summary>
public sealed class OverlayManager
{
    private readonly ConfigStore _store;
    private readonly MotionSource _motion = new();
    private readonly List<OverlayWindow> _windows = new();
    private readonly DispatcherTimer _windowFollowTimer;
    private readonly DispatcherTimer _visibilityTimer;
    private readonly DispatcherTimer _motionTimer;
    private DateTime _lastMotionTick;
    private readonly uint _processId = (uint)Environment.ProcessId;
    private bool _enabled;
    private string _lastTargetKey = "";
    private Rectangle _lastFollowRect;
    private DateTime _breakNoticeUntil;

    public OverlayManager(ConfigStore store)
    {
        _store = store;
        _store.Changed += OnConfigChanged;
        _windowFollowTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _windowFollowTimer.Tick += (_, _) => UpdateWindowState();
        _visibilityTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _visibilityTimer.Tick += (_, _) => UpdateWindowState();
        _motionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(8) };
        _motionTimer.Tick += (_, _) => TickMotion();
    }

    public bool IsActive => _enabled && _windows.Count > 0;

    public void Start()
    {
        _enabled = _store.State.OverlayEnabled;
        _lastTargetKey = "";
        Rebuild();
        _visibilityTimer.Start();
        _lastMotionTick = DateTime.UtcNow;
        _motionTimer.Start();
        UpdateWindowState();
    }

    public void Stop()
    {
        _enabled = false;
        _windowFollowTimer.Stop();
        _visibilityTimer.Stop();
        _motionTimer.Stop();
        CloseAll();
    }

    public void Toggle()
    {
        _store.Mutate(c => c.OverlayEnabled = !c.OverlayEnabled);
    }

    private void OnConfigChanged()
    {
        var st = _store.State;
        if (st.OverlayEnabled != _enabled)
        {
            if (st.OverlayEnabled) Start(); else Stop();
            return;
        }
        var key = st.Target.ToString();
        if (key != _lastTargetKey)
        {
            Rebuild();
            if (st.Target == OverlayTarget.ActiveWindow) _windowFollowTimer.Start();
            else _windowFollowTimer.Stop();
        }
        else
        {
            foreach (var w in _windows) w.Refresh();
        }
        UpdateWindowState();
    }

    private void Rebuild()
    {
        CloseAll();
        if (!_enabled) return;
        var st = _store.State;
        _lastTargetKey = st.Target.ToString();

        var zones = ComputeZones(st.Target);
        foreach (var z in zones)
        {
            var w = new OverlayWindow(_store, _motion, z.Left, z.Top, z.Width, z.Height);
            _windows.Add(w);
            w.Show();
        }
        if (st.Target == OverlayTarget.ActiveWindow)
            UpdateWindowState();
    }

    private void CloseAll()
    {
        foreach (var w in _windows) w.Close();
        _windows.Clear();
    }

    private List<Rectangle> ComputeZones(OverlayTarget target)
    {
        var result = new List<Rectangle>();
        switch (target)
        {
            case OverlayTarget.Primary:
                result.Add(Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080));
                break;
            case OverlayTarget.ActiveWindow:
                result.Add(GetForegroundRect() ?? Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080));
                break;
            default:
                foreach (var s in Screen.AllScreens)
                    result.Add(s.Bounds);
                if (result.Count == 0) result.Add(new Rectangle(0, 0, 1920, 1080));
                break;
        }
        return result;
    }

    private Rectangle? GetForegroundRect()
    {
        var hwnd = Win32Native.GetForegroundWindow();
        if (hwnd == IntPtr.Zero || !Win32Native.IsWindowVisible(hwnd)) return null;
        if (!Win32Native.GetWindowRect(hwnd, out var r) || r.Width < 200 || r.Height < 200) return null;
        return new Rectangle(r.Left, r.Top, r.Width, r.Height);
    }

    private void UpdateWindowState()
    {
        if (_windows.Count == 0) return;
        var breakNoticeActive = _breakNoticeUntil > DateTime.Now;
        if (ShouldHideOverlay(_enabled && _store.State.OverlayEnabled, IsOwnWindowForeground(), breakNoticeActive))
        {
            foreach (var w in _windows) w.Hide();
            return;
        }

        if (_store.State.Target == OverlayTarget.ActiveWindow)
        {
            var rect = GetForegroundRect();
            if (rect == null)
            {
                foreach (var w in _windows) w.Hide();
                return;
            }
            if (_lastFollowRect != rect.Value)
            {
                _lastFollowRect = rect.Value;
                foreach (var w in _windows) w.SetBounds(rect.Value.Left, rect.Value.Top, rect.Value.Width, rect.Value.Height);
            }
        }

        foreach (var w in _windows)
        {
            if (!w.IsVisible) w.Show();
        }
    }

    private bool IsOwnWindowForeground()
    {
        var hwnd = Win32Native.GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return false;
        Win32Native.GetWindowThreadProcessId(hwnd, out var pid);
        return pid == _processId;
    }

    private void TickMotion()
    {
        var now = DateTime.UtcNow;
        var dt = Math.Clamp((now - _lastMotionTick).TotalSeconds, 0, 0.1);
        _lastMotionTick = now;
        _motion.Tick(dt, _store.State.DotMode);
    }

    public void ShowBreakNotice(int seconds)
    {
        _breakNoticeUntil = DateTime.Now.AddSeconds(seconds);
        foreach (var w in _windows) w.ShowBreakNotice(seconds);
        UpdateWindowState();
    }

    private static bool ShouldHideOverlay(bool enabled, bool ownWindowForeground, bool breakNoticeActive) =>
        !enabled || ownWindowForeground && !breakNoticeActive;
}
