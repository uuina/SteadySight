using SteadySight.Models;

namespace SteadySight.Overlay;

/// <summary>屏幕空间的纯几何计算，不依赖 WPF，便于单元测试。</summary>
public static class OverlayGeometry
{
    public readonly record struct Region(double X, double Y, double Width, double Height);

    /// <summary>
    /// 边缘遮罩区域：将屏幕四周按宽度比例裁出四条梯形带，中央保留可视矩形。
    /// 这与动态 FOV 限制的思路一致（降低周边光流），但以柔和遮罩呈现。
    /// </summary>
    public static IReadOnlyList<Region> ComputeEdgeBands(double w, double h, double widthPercent)
    {
        var inset = w * (widthPercent / 100.0);
        var vInset = h * (widthPercent / 100.0 * 0.8);
        var cx = w / 2.0;
        var cy = h / 2.0;
        return new List<Region>
        {
            // 上
            new(0, 0, w, vInset),
            // 下
            new(0, h - vInset, w, vInset),
            // 左（扣除上下重叠）
            new(0, vInset, inset, Math.Max(0, h - 2 * vInset)),
            // 右
            new(w - inset, vInset, inset, Math.Max(0, h - 2 * vInset))
        };
    }

    /// <summary>四角 L 形取景框（FPS 风格：只保留四角，中心完全透明）。</summary>
    public static IReadOnlyList<Region> ComputeCornerFrames(double w, double h, double widthPercent)
    {
        var len = Math.Min(w, h) * (widthPercent / 100.0 * 2.2);
        var thick = Math.Max(2.0, len * 0.09);
        (double X, double Y, double HorizontalY, double VerticalX)[] corners =
        {
            (0, 0, 0, 0),
            (w - len, 0, 0, w - thick),
            (0, h - len, h - thick, 0),
            (w - len, h - len, h - thick, w - thick)
        };
        var list = new List<Region>();
        foreach (var (x, y, horizontalY, verticalX) in corners)
        {
            list.Add(new Region(x, horizontalY, len, thick));
            list.Add(new Region(verticalX, y, thick, len));
        }
        return list;
    }

    /// <summary>四边完整参考线框。</summary>
    public static IReadOnlyList<Region> ComputeFullFrame(double w, double h, double widthPercent)
    {
        var thick = Math.Max(1.5, Math.Min(w, h) * (widthPercent / 100.0 * 0.08));
        return new List<Region>
        {
            new(0, 0, w, thick),
            new(0, h - thick, w, thick),
            new(0, 0, thick, h),
            new(w - thick, 0, thick, h)
        };
    }

    /// <summary>水平参考线 y 坐标（含偏移百分比）。</summary>
    public static double HorizonY(double h, double offsetPercent) =>
        h / 2.0 + h * (offsetPercent / 100.0);

    /// <summary>锚点配置使用直径像素，渲染器使用半径。</summary>
    public static double AnchorRadius(double size) => Math.Max(1.0, size / 2.0);

    /// <summary>
    /// 动态圆点视差缩放：靠近水平中线时缩小，营造“物体在眼前经过”的深度感。
    /// </summary>
    public static double ParallaxScale(double x, double w)
    {
        var dx = Math.Abs(x - w / 2.0) / Math.Max(1.0, w / 2.0);
        return 0.45 + 0.55 * dx;
    }

    /// <summary>根据视点速度把圆点向反方向移动，模拟外围稳定参照物。</summary>
    public static (double Dx, double Dy) OppositeDrift(float vx, float vy, double dt, double intensity) =>
        (-vx * dt * intensity * 36.0, -vy * dt * intensity * 36.0);
}
