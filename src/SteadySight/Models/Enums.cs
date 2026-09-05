namespace SteadySight.Models;

public enum EdgeStyle
{
    Off,
    SoftShade,      // 柔和外围遮罩（动态 FOV 限制的温和版）
    CornerFrame,    // 四角取景框
    FullFrame       // 四边参考线框
}

public enum AnchorStyle
{
    Off,
    Dot,
    Ring,
    Cross,
    CircleDot
}

public enum HorizonStyle
{
    Off,
    Line,
    LineTick
}

public enum DotMotionMode
{
    Off,
    Mouse,
    MouseGamepad,
    Auto
}

public enum OverlayTarget
{
    AllMonitors,
    Primary,
    ActiveWindow
}

public enum FrameRatePreset
{
    PowerSave30,
    Standard60,
    Smooth120
}

public enum Language
{
    ZhCN,
    EnUS
}

public enum BreakInterval
{
    Off,
    Every15,
    Every30,
    Every45,
    Every60
}
