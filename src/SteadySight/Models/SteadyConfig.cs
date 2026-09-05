namespace SteadySight.Models;

public sealed class SteadyConfig
{
    public bool OverlayEnabled { get; set; } = true;

    // 边缘稳定
    public EdgeStyle EdgeStyle { get; set; } = EdgeStyle.SoftShade;
    public double EdgeWidthPercent { get; set; } = 14;      // 4..38
    public double EdgeOpacity { get; set; } = 0.42;         // 0.05..0.85

    // 中心锚点
    public AnchorStyle AnchorStyle { get; set; } = AnchorStyle.Dot;
    public double AnchorSize { get; set; } = 22;            // 6..64
    public double AnchorOpacity { get; set; } = 0.55;
    public string AnchorColor { get; set; } = "#7CF5C4";

    // 动态圆点（人工光流）
    public DotMotionMode DotMode { get; set; } = DotMotionMode.Auto;
    public double DotDensity { get; set; } = 0.55;          // 0.1..1
    public double DotSize { get; set; } = 3.2;              // 1..8
    public double DotIntensity { get; set; } = 0.6;         // 灵敏度 0..1
    public bool DotParallax { get; set; } = true;           // 近中心缩小
    public bool DotFadeOnIdle { get; set; } = true;
    public double DotMaxOpacity { get; set; } = 0.5;

    // 水平参考线
    public HorizonStyle HorizonStyle { get; set; } = HorizonStyle.Off;
    public double HorizonOpacity { get; set; } = 0.22;
    public double HorizonOffsetPercent { get; set; } = 0;   // -25..25

    // 运行
    public OverlayTarget Target { get; set; } = OverlayTarget.AllMonitors;
    public FrameRatePreset FpsPreset { get; set; } = FrameRatePreset.Standard60;
    public bool GameHotkeysEnabled { get; set; } = true;
    public BreakInterval BreakEvery { get; set; } = BreakInterval.Every30;
    public Language Language { get; set; } = Language.ZhCN;
    public string ProfileName { get; set; } = "标准";

    public SteadyConfig Clone() => (SteadyConfig)MemberwiseClone();
}
