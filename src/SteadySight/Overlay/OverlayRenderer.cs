using System.Windows;
using System.Windows.Media;
using SteadySight.Models;
using SteadySight.Services;

namespace SteadySight.Overlay;

/// <summary>
/// 纯软件自绘渲染器：每帧生成 DrawingVisual。
/// 无 D3D 资源与位图缓存。
/// </summary>
public sealed class OverlayRenderer
{
    private readonly ConfigStore _store;
    private readonly MotionSource _motion;
    private readonly Random _rng = new(20260905);

    private double _w, _h;

    // 圆点状态
    private readonly List<double> _dotX = new();
    private readonly List<double> _dotY = new();
    private readonly List<double> _dotVariation = new();
    private readonly List<int> _dotSide = new();
    private bool _dotsInit;
    private double _lastDotSize = -1;

    // 休息提示状态
    private DateTime _breakUntil = DateTime.MinValue;

    public OverlayRenderer(ConfigStore store, MotionSource motion)
    {
        _store = store;
        _motion = motion;
    }

    public void SetSize(double w, double h)
    {
        if (Math.Abs(_w - w) < 0.1 && Math.Abs(_h - h) < 0.1) return;
        _w = w;
        _h = h;
        _dotsInit = false;
    }

    public void ShowBreakNotice(int seconds) =>
        _breakUntil = DateTime.Now.AddSeconds(seconds);

    public void Render(DrawingContext dc, double dtSeconds)
    {
        if (_w <= 0 || _h <= 0) return;
        var cfg = _store.State;
        EnsureDots(cfg);

        var w = _w;
        var h = _h;

        DrawEdge(cfg, dc, w, h);
        DrawHorizon(cfg, dc, w, h);
        DrawDots(cfg, dc, w, h, dtSeconds);
        DrawAnchor(cfg, dc, w, h);
        DrawBreakNotice(cfg, dc, w, h);
    }

    private void DrawEdge(SteadyConfig cfg, DrawingContext dc, double w, double h)
    {
        if (cfg.EdgeStyle == EdgeStyle.Off || cfg.EdgeOpacity <= 0.01) return;
        IReadOnlyList<OverlayGeometry.Region> regions = cfg.EdgeStyle switch
        {
            EdgeStyle.SoftShade => OverlayGeometry.ComputeEdgeBands(w, h, cfg.EdgeWidthPercent),
            EdgeStyle.CornerFrame => OverlayGeometry.ComputeCornerFrames(w, h, cfg.EdgeWidthPercent),
            EdgeStyle.FullFrame => OverlayGeometry.ComputeFullFrame(w, h, cfg.EdgeWidthPercent),
            _ => Array.Empty<OverlayGeometry.Region>()
        };

        if (cfg.EdgeStyle == EdgeStyle.SoftShade)
        {
            var alpha = (byte)Math.Round(255 * cfg.EdgeOpacity);
            var top = new LinearGradientBrush(
                Color.FromArgb(alpha, 0, 0, 0), Colors.Transparent, new Point(0.5, 0), new Point(0.5, 1));
            var bottom = new LinearGradientBrush(
                Colors.Transparent, Color.FromArgb(alpha, 0, 0, 0), new Point(0.5, 0), new Point(0.5, 1));
            var left = new LinearGradientBrush(
                Color.FromArgb(alpha, 0, 0, 0), Colors.Transparent, new Point(0, 0.5), new Point(1, 0.5));
            var right = new LinearGradientBrush(
                Colors.Transparent, Color.FromArgb(alpha, 0, 0, 0), new Point(0, 0.5), new Point(1, 0.5));
            top.Freeze(); bottom.Freeze(); left.Freeze(); right.Freeze();
            dc.DrawRectangle(top, null, new Rect(regions[0].X, regions[0].Y, regions[0].Width, regions[0].Height));
            dc.DrawRectangle(bottom, null, new Rect(regions[1].X, regions[1].Y, regions[1].Width, regions[1].Height));
            dc.DrawRectangle(left, null, new Rect(regions[2].X, regions[2].Y, regions[2].Width, regions[2].Height));
            dc.DrawRectangle(right, null, new Rect(regions[3].X, regions[3].Y, regions[3].Width, regions[3].Height));
        }
        else
        {
            var frameBrush = new SolidColorBrush(Color.FromArgb(
                (byte)Math.Round(255 * Math.Min(0.9, cfg.EdgeOpacity * 1.35)), 215, 249, 234));
            frameBrush.Freeze();
            foreach (var r in regions)
                dc.DrawRectangle(frameBrush, null, new Rect(r.X, r.Y, r.Width, r.Height));
        }
    }

    private void DrawHorizon(SteadyConfig cfg, DrawingContext dc, double w, double h)
    {
        if (cfg.HorizonStyle == HorizonStyle.Off || cfg.HorizonOpacity <= 0.01) return;
        var y = OverlayGeometry.HorizonY(h, cfg.HorizonOffsetPercent);
        var pen = new Pen(Brushes.White, 2.0)
        {
            Brush = new SolidColorBrush(Color.FromArgb(
                (byte)Math.Round(255 * cfg.HorizonOpacity), 255, 255, 255)),
            DashStyle = cfg.HorizonStyle == HorizonStyle.LineTick ? DashStyles.Dash : DashStyles.Solid
        };
        pen.Freeze();
        dc.DrawLine(pen, new Point(0, y), new Point(w, y));
    }

    private void EnsureDots(SteadyConfig cfg)
    {
        if (_w <= 0 || _h <= 0) return;
        var count = (int)Math.Round(cfg.DotDensity * 52 * _w * _h / (1920.0 * 1080.0));
        count = Math.Clamp(count, 6, 160);
        if (_dotsInit && count == _dotX.Count && Math.Abs(_lastDotSize - cfg.DotSize) < 0.001) return;
        _dotX.Clear(); _dotY.Clear();
        _dotVariation.Clear(); _dotSide.Clear();
        var band = Math.Clamp(Math.Min(_w, _h) * 0.14, 48, 180);
        for (var i = 0; i < count; i++)
        {
            var side = i % 4;
            _dotSide.Add(side);
            switch (side)
            {
                case 0: // top
                    _dotX.Add(_rng.NextDouble() * _w);
                    _dotY.Add(_rng.NextDouble() * band);
                    break;
                case 1: // right
                    _dotX.Add(Math.Max(0, _w - band) + _rng.NextDouble() * band);
                    _dotY.Add(band + _rng.NextDouble() * Math.Max(1, _h - band * 2));
                    break;
                case 2: // bottom
                    _dotX.Add(_rng.NextDouble() * _w);
                    _dotY.Add(Math.Max(0, _h - band) + _rng.NextDouble() * band);
                    break;
                default: // left
                    _dotX.Add(_rng.NextDouble() * band);
                    _dotY.Add(band + _rng.NextDouble() * Math.Max(1, _h - band * 2));
                    break;
            }
            _dotVariation.Add(0.7 + 0.7 * _rng.NextDouble());
        }
        _lastDotSize = cfg.DotSize;
        _dotsInit = true;
    }

    private void DrawDots(SteadyConfig cfg, DrawingContext dc, double w, double h, double dt)
    {
        if (cfg.DotMode == DotMotionMode.Off || cfg.DotDensity <= 0.01 || _dotX.Count == 0) return;

        var hasSignal = _motion.HasSignal;
        var fadeOnIdle = ShouldFadeOnIdle(cfg.DotMode, cfg.DotFadeOnIdle);
        var targetOpacity = !fadeOnIdle || hasSignal ? cfg.DotMaxOpacity : 0.0;
        _lastDotsGlow += (float)((targetOpacity - _lastDotsGlow) * Math.Min(1, dt * 4.0));
        if (_lastDotsGlow <= 0.003) return;

        var vx = _motion.Vx;
        var vy = _motion.Vy;
        var (dxx, dyy) = OverlayGeometry.OppositeDrift(vx, vy, dt, cfg.DotIntensity);

        var dotBrush = new SolidColorBrush(Color.FromArgb(
            (byte)Math.Clamp(_lastDotsGlow * 255 * 0.6, 0, 255), 124, 245, 196));
        dotBrush.Freeze();
        var band = Math.Clamp(Math.Min(w, h) * 0.14, 48, 180);
        for (var i = 0; i < _dotX.Count; i++)
        {
            switch (_dotSide[i])
            {
                case 0:
                    _dotX[i] = Wrap(_dotX[i] + dxx, w);
                    _dotY[i] = Math.Clamp(_dotY[i] + dyy * 0.08, 0, band);
                    break;
                case 1:
                    _dotX[i] = Math.Clamp(_dotX[i] + dxx * 0.08, Math.Max(0, w - band), w);
                    _dotY[i] = Wrap(_dotY[i] + dyy, h);
                    break;
                case 2:
                    _dotX[i] = Wrap(_dotX[i] + dxx, w);
                    _dotY[i] = Math.Clamp(_dotY[i] + dyy * 0.08, Math.Max(0, h - band), h);
                    break;
                default:
                    _dotX[i] = Math.Clamp(_dotX[i] + dxx * 0.08, 0, band);
                    _dotY[i] = Wrap(_dotY[i] + dyy, h);
                    break;
            }

            var scale = cfg.DotParallax
                ? OverlayGeometry.ParallaxScale(_dotX[i], w)
                : 1.0;
            var radius = cfg.DotSize * _dotVariation[i] * scale;
            dc.DrawEllipse(dotBrush, null, new Point(_dotX[i], _dotY[i]), radius, radius);
        }
    }

    private float _lastDotsGlow;

    private static bool ShouldFadeOnIdle(DotMotionMode mode, bool fadeSetting) =>
        mode == DotMotionMode.Auto || fadeSetting;

    private static double Wrap(double v, double limit)
    {
        if (v < 0) return v + limit * Math.Ceiling(-v / limit);
        if (v >= limit) return v % limit;
        return v;
    }

    private void DrawAnchor(SteadyConfig cfg, DrawingContext dc, double w, double h)
    {
        if (cfg.AnchorStyle == AnchorStyle.Off || cfg.AnchorOpacity <= 0.01) return;
        var c = ParseColor(cfg.AnchorColor, 124, 245, 196);
        var alpha = (byte)Math.Round(255 * cfg.AnchorOpacity);
        var brush = new SolidColorBrush(Color.FromArgb(alpha, c.R, c.G, c.B));
        brush.Freeze();
        var pen = new Pen(brush, Math.Max(1.6, cfg.AnchorSize * 0.09));
        pen.Freeze();
        var center = new Point(w / 2, h / 2);
        var r = OverlayGeometry.AnchorRadius(cfg.AnchorSize);

        switch (cfg.AnchorStyle)
        {
            case AnchorStyle.Dot:
                dc.DrawEllipse(brush, null, center, r * 0.8, r * 0.8);
                break;
            case AnchorStyle.Ring:
                dc.DrawEllipse(null, pen, center, r, r);
                break;
            case AnchorStyle.CircleDot:
                dc.DrawEllipse(null, pen, center, r, r);
                dc.DrawEllipse(brush, null, center, r * 0.35, r * 0.35);
                break;
            case AnchorStyle.Cross:
                dc.DrawLine(pen, new Point(center.X - r, center.Y), new Point(center.X + r, center.Y));
                dc.DrawLine(pen, new Point(center.X, center.Y - r), new Point(center.X, center.Y + r));
                break;
        }
    }

    private void DrawBreakNotice(SteadyConfig cfg, DrawingContext dc, double w, double h)
    {
        if (_breakUntil <= DateTime.Now) return;
        var remaining = Math.Round((_breakUntil - DateTime.Now).TotalSeconds);
        if (remaining <= 0) return;

        var font = new Typeface("Microsoft YaHei");
        var text = $"休息一下 · {remaining}s";
        var ft = new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, font, 22, Brushes.White, 1.25);
        var panelH = 54.0;
        var panelW = ft.Width + 48;
        if (panelW > w - 24) { panelW = w - 24; }
        var x = (w - panelW) / 2;
        var y = 22;
        var brush = new SolidColorBrush(Color.FromArgb(210, 0, 0, 0));
        brush.Freeze();
        dc.DrawRoundedRectangle(brush, null, new Rect(x, y, panelW, panelH), 12, 12);
        var textBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        textBrush.Freeze();
        dc.DrawText(ft, new Point(x + (panelW - ft.Width) / 2, y + (panelH - ft.Height) / 2));
    }

    private static (byte R, byte G, byte B) ParseColor(string hex, byte dr, byte dg, byte db)
    {
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            return (c.R, c.G, c.B);
        }
        catch
        {
            return (dr, dg, db);
        }
    }
}
