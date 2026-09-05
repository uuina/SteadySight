using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using SteadySight.Services;

namespace SteadySight.Overlay;

public partial class OverlayWindow : Window
{
    private readonly ConfigStore _store;
    private readonly MotionSource _motion;
    private readonly OverlayRenderer _renderer;
    private HwndSource? _source;
    private readonly DispatcherTimer _renderTimer = new();
    private bool _rawAttached;
    private int _physicalX;
    private int _physicalY;
    private int _physicalWidth;
    private int _physicalHeight;
    private double _monitorScale;

    public OverlayWindow(ConfigStore store, MotionSource motion, int x, int y, int w, int h)
    {
        _store = store;
        _motion = motion;
        _renderer = new OverlayRenderer(store, motion);
        InitializeComponent();

        _physicalX = x;
        _physicalY = y;
        _physicalWidth = Math.Max(1, w);
        _physicalHeight = Math.Max(1, h);
        _monitorScale = Win32Native.GetMonitorScale(x, y);
        Left = 0;
        Top = 0;
        Width = _physicalWidth / _monitorScale;
        Height = _physicalHeight / _monitorScale;
        _renderer.SetSize(Width, Height);

        Content = new OverlayHost(_renderer);
        Loaded += OnLoaded;
        Closed += OnClosed;
        _store.Changed += OnConfigChanged;
        UpdateFrameInterval();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        Win32Native.MakeClickThrough(helper.Handle);
        Win32Native.SetWindowPos(helper.Handle, Win32Native.HwndTopmost,
            _physicalX, _physicalY, _physicalWidth, _physicalHeight,
            Win32Native.SwpNoActivate | Win32Native.SwpShowWindow);
        _source = HwndSource.FromHwnd(helper.Handle);
        _source?.AddHook(WndProc);
        if (!_rawAttached)
        {
            _rawAttached = _motion.AttachRawInput(helper.Handle);
        }
        StartRendering();
    }

    private void StartRendering()
    {
        UpdateFrameInterval();
        _renderTimer.Tick += OnFrame;
        _renderTimer.Start();
    }

    private void OnConfigChanged()
    {
        UpdateFrameInterval();
    }

    private void UpdateFrameInterval()
    {
        var frameIntervalMs = _store.State.FpsPreset switch
        {
            Models.FrameRatePreset.PowerSave30 => 33.33,
            Models.FrameRatePreset.Smooth120 => 8.33,
            _ => 16.67
        };
        _renderTimer.Interval = TimeSpan.FromMilliseconds(frameIntervalMs);
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        ((OverlayHost)Content).InvalidateVisual();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32Native.WmInput)
        {
            uint size = 0;
            Win32Native.GetRawInputData(lParam, 0x10000003, IntPtr.Zero, ref size, (uint)Marshal.SizeOf<Win32Native.RawInputHeader>());
            if (size > 0)
            {
                var data = Marshal.AllocHGlobal((int)size);
                try
                {
                    if (Win32Native.GetRawInputData(lParam, 0x10000003, data, ref size,
                            (uint)Marshal.SizeOf<Win32Native.RawInputHeader>()) == size)
                    {
                        var raw = Marshal.PtrToStructure<Win32Native.RawInput>(data);
                        _motion.HandleMessage(raw, hwnd);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(data);
                }
            }
            handled = true;
            return IntPtr.Zero;
        }
        return IntPtr.Zero;
    }

    public void SetBounds(int x, int y, int w, int h)
    {
        _physicalX = x;
        _physicalY = y;
        _physicalWidth = Math.Max(1, w);
        _physicalHeight = Math.Max(1, h);
        _monitorScale = Win32Native.GetMonitorScale(x, y);
        Width = _physicalWidth / _monitorScale;
        Height = _physicalHeight / _monitorScale;
        _renderer.SetSize(Width, Height);
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            Win32Native.SetWindowPos(hwnd, Win32Native.HwndTopmost,
                _physicalX, _physicalY, _physicalWidth, _physicalHeight,
                Win32Native.SwpNoActivate | Win32Native.SwpShowWindow);
        }
    }

    public void Refresh()
    {
        ((OverlayHost)Content)?.InvalidateVisual();
    }

    public void ShowBreakNotice(int seconds) => _renderer.ShowBreakNotice(seconds);

    private void OnClosed(object? sender, EventArgs e)
    {
        _renderTimer.Stop();
        _renderTimer.Tick -= OnFrame;
        _store.Changed -= OnConfigChanged;
        if (_source != null)
        {
            _source.RemoveHook(WndProc);
            _source = null;
        }
    }
}

/// <summary>每帧在 OnRender 中用 DrawingContext 直接绘制叠加内容。</summary>
internal sealed class OverlayHost : FrameworkElement
{
    private readonly OverlayRenderer _renderer;
    private readonly System.Diagnostics.Stopwatch _sw = new();

    public OverlayHost(OverlayRenderer renderer)
    {
        _renderer = renderer;
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (!_sw.IsRunning) _sw.Start();
        var dt = _sw.Elapsed.TotalSeconds;
        _sw.Restart();
        _renderer.SetSize(Math.Max(1, ActualWidth), Math.Max(1, ActualHeight));
        _renderer.Render(drawingContext, dt);
    }
}
