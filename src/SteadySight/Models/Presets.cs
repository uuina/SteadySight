namespace SteadySight.Models;

public sealed record PresetDef(string Key, string ZhName, string EnName, Action<SteadyConfig> Apply);

public static class Presets
{
    public static readonly PresetDef[] All =
    {
        new("standard", "标准", "Standard", c =>
        {
            c.EdgeStyle = EdgeStyle.SoftShade;
            c.AnchorStyle = AnchorStyle.Dot;
            c.DotMode = DotMotionMode.Auto;
            c.HorizonStyle = HorizonStyle.Off;
        }),
        new("strong", "强力", "Strong", c =>
        {
            c.EdgeStyle = EdgeStyle.SoftShade;
            c.EdgeWidthPercent = 22; c.EdgeOpacity = 0.6;
            c.AnchorStyle = AnchorStyle.CircleDot; c.AnchorSize = 20; c.AnchorOpacity = 0.7;
            c.DotMode = DotMotionMode.MouseGamepad; c.DotIntensity = 0.8;
            c.HorizonStyle = HorizonStyle.Off;
        }),
        new("fps", "射击", "FPS", c =>
        {
            c.EdgeStyle = EdgeStyle.CornerFrame;
            c.EdgeWidthPercent = 12; c.EdgeOpacity = 0.3;
            c.AnchorStyle = AnchorStyle.Ring; c.AnchorSize = 18; c.AnchorOpacity = 0.6;
            c.DotMode = DotMotionMode.Mouse; c.DotIntensity = 0.7;
            c.HorizonStyle = HorizonStyle.Off;
        }),
        new("drive", "驾驶", "Driving", c =>
        {
            c.EdgeStyle = EdgeStyle.FullFrame;
            c.EdgeWidthPercent = 10; c.EdgeOpacity = 0.24;
            c.AnchorStyle = AnchorStyle.Dot; c.AnchorSize = 10; c.AnchorOpacity = 0.45;
            c.DotMode = DotMotionMode.MouseGamepad; c.DotIntensity = 0.6;
            c.HorizonStyle = HorizonStyle.LineTick; c.HorizonOpacity = 0.3;
        }),
        new("cinema", "观影", "Cinema", c =>
        {
            c.EdgeStyle = EdgeStyle.SoftShade;
            c.EdgeWidthPercent = 10; c.EdgeOpacity = 0.5;
            c.AnchorStyle = AnchorStyle.Off;
            c.DotMode = DotMotionMode.Off;
            c.HorizonStyle = HorizonStyle.Off;
        }),
        new("minimal", "极简", "Minimal", c =>
        {
            c.EdgeStyle = EdgeStyle.Off;
            c.AnchorStyle = AnchorStyle.Dot; c.AnchorSize = 10; c.AnchorOpacity = 0.4;
            c.DotMode = DotMotionMode.Off;
            c.HorizonStyle = HorizonStyle.Off;
        })
    };

    public static PresetDef? Find(string key) =>
        All.FirstOrDefault(p => p.Key == key);

    public static bool Apply(SteadyConfig config, string key)
    {
        var preset = Find(key);
        if (preset == null) return false;

        ResetVisualSettings(config);
        preset.Apply(config);
        config.ProfileName = preset.ZhName;
        return true;
    }

    private static void ResetVisualSettings(SteadyConfig config)
    {
        var defaults = new SteadyConfig();
        config.EdgeStyle = defaults.EdgeStyle;
        config.EdgeWidthPercent = defaults.EdgeWidthPercent;
        config.EdgeOpacity = defaults.EdgeOpacity;
        config.AnchorStyle = defaults.AnchorStyle;
        config.AnchorSize = defaults.AnchorSize;
        config.AnchorOpacity = defaults.AnchorOpacity;
        config.AnchorColor = defaults.AnchorColor;
        config.DotMode = defaults.DotMode;
        config.DotDensity = defaults.DotDensity;
        config.DotSize = defaults.DotSize;
        config.DotIntensity = defaults.DotIntensity;
        config.DotParallax = defaults.DotParallax;
        config.DotFadeOnIdle = defaults.DotFadeOnIdle;
        config.DotMaxOpacity = defaults.DotMaxOpacity;
        config.HorizonStyle = defaults.HorizonStyle;
        config.HorizonOpacity = defaults.HorizonOpacity;
        config.HorizonOffsetPercent = defaults.HorizonOffsetPercent;
    }
}
