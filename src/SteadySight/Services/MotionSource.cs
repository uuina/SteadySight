using System.Windows;
using System.Runtime.InteropServices;
using SteadySight.Models;

namespace SteadySight.Services;

/// <summary>
/// 汇聚鼠标 RawInput 与手柄摇杆，输出平滑的“视点移动速度”。
/// 正 X = 视点向右转（场景向左流），圆点据此反向漂移。
/// </summary>
public sealed class MotionSource
{
    private readonly object _gate = new();
    private float _vx, _vy, _gx, _gy;
    private IntPtr _rawInputTarget;

    public bool HasSignal { get; private set; }
    public float Vx { get; private set; }
    public float Vy { get; private set; }

    internal bool AttachRawInput(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;
        if (_rawInputTarget == hwnd) return true;
        var dev = new[] { new Win32Native.RawInputDevice
        {
            UsagePage = 0x01, Usage = 0x02,
            Flags = Win32Native.RidEvInputSink,
            Target = hwnd
        } };
        var ok = Win32Native.RegisterRawInputDevices(dev, 1,
            (uint)Marshal.SizeOf<Win32Native.RawInputDevice>());
        if (ok) _rawInputTarget = hwnd;
        return ok;
    }

    internal bool HandleMessage(Win32Native.RawInput raw, IntPtr hwnd)
    {
        if (raw.Header.Type != Win32Native.RidInputMouse) return false;
        int dx = (int)raw.Mouse.LastX;
        int dy = (int)raw.Mouse.LastY;
        if (dx == 0 && dy == 0) return false;

        int dpi = Win32Native.GetDpiForWindow(hwnd);
        if (dpi <= 0) dpi = 96;
        float scale = 96f / dpi;
        float k = 0.85f;
        lock (_gate)
        {
            _vx = _vx * 0.82f + dx * k * scale;
            _vy = _vy * 0.82f + dy * k * scale;
        }
        return true;
    }

    /// <summary>每帧调用：平滑衰减并入手柄速度。</summary>
    public void Tick(double dtSeconds, DotMotionMode mode)
    {
        float gx = 0, gy = 0;
        if (mode is DotMotionMode.Auto or DotMotionMode.MouseGamepad && TryReadGamepad(out gx, out gy))
        {
            lock (_gate)
            {
                _gx = _gx * 0.6f + gx * 0.4f;
                _gy = _gy * 0.6f + gy * 0.4f;
            }
        }

        lock (_gate)
        {
            float decay = MathF.Max(0f, 1f - 7f * (float)dtSeconds);
            _vx *= decay; _vy *= decay;
            float gd = MathF.Max(0f, 1f - 3f * (float)dtSeconds);
            _gx *= gd; _gy *= gd;
            var useGamepad = mode is DotMotionMode.Auto or DotMotionMode.MouseGamepad;
            Vx = _vx + (useGamepad ? _gx * 260f : 0f);
            Vy = _vy + (useGamepad ? _gy * 260f : 0f);
            float mag = MathF.Abs(Vx) + MathF.Abs(Vy);
            HasSignal = mag > 0.4f;
        }
    }

    private static bool TryReadGamepad(out float gx, out float gy)
    {
        gx = 0; gy = 0;
        var rc = Win32Native.XInputGetState(0, out var st);
        if (rc != 0) return false;
        return ReadCameraStick(
            st.Gamepad.ThumbLX, st.Gamepad.ThumbLY,
            st.Gamepad.ThumbRX, st.Gamepad.ThumbRY,
            out gx, out gy);
    }

    private static bool ReadCameraStick(
        short leftX, short leftY, short rightX, short rightY,
        out float gx, out float gy)
    {
        gx = 0; gy = 0;
        const float dead = 4000f;
        float rx = rightX, ry = rightY;
        float mag = MathF.Sqrt(rx * rx + ry * ry);
        if (mag < dead) return false;
        float norm = (mag - dead) / (32767f - dead);
        gx = rx / mag * norm;
        gy = -ry / mag * norm;
        return true;
    }
}
