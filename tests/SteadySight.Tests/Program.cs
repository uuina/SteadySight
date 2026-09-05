using System.Reflection;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SteadySight;
using SteadySight.Models;
using SteadySight.Overlay;
using SteadySight.Services;
using SteadySight.Views;

namespace SteadySight.Tests;

internal static class Program
{
    private static readonly List<(string Name, Action Test)> Tests =
    [
        ("导航可以从总览切换到边缘稳定", NavigationChangesPage),
        ("百分比滑块使用界面显示的百分数单位", PercentageSlidersUsePercentUnits),
        ("禁用全局快捷键时不占用系统按键", DisabledHotkeysAreNotRegistered),
        ("无关配置变更不会重置休息提醒计时", UnrelatedConfigDoesNotResetBreakTimer),
        ("帧率配置会立即更新现有叠加窗口", FrameRateChangeUpdatesExistingWindow),
        ("Raw Input 可以重新绑定到重建后的窗口", RawInputRebindsToNewWindow),
        ("同一运行锁只能被一个实例取得", SingleInstanceLockIsExclusive),
        ("手柄视角输入读取右摇杆", GamepadCameraMotionUsesRightStick),
        ("四角取景框线段贴合屏幕四角", CornerFramesTouchScreenEdges),
        ("中心锚点尺寸按配置值缩放", AnchorRadiusTracksConfiguredSize),
        ("智能圆点模式始终在静止时淡出", AutoDotsAlwaysFadeWhenIdle),
        ("休息提示可在设置窗口前台时显示", BreakNoticeCanShowOverSettings),
        ("标准预设与默认视觉参数一致", StandardPresetMatchesDefaults),
        ("预设结果不依赖之前使用的预设", PresetApplicationIsDeterministic)
    ];

    [STAThread]
    private static int Main(string[] args)
    {
        var app = new App();
        app.InitializeComponent();

        if (args is ["--capture", var output])
        {
            CapturePages(output);
            return 0;
        }

        var failures = new List<string>();
        foreach (var (name, test) in Tests)
        {
            try
            {
                test();
                Console.WriteLine($"PASS {name}");
            }
            catch (Exception ex)
            {
                failures.Add(name);
                Console.WriteLine($"FAIL {name}: {ex.Message}");
            }
        }

        Console.WriteLine($"\n结果: {Tests.Count - failures.Count}/{Tests.Count} 通过");
        return failures.Count == 0 ? 0 : 1;
    }

    private static void NavigationChangesPage()
    {
        SetAppStore(CreateStore());
        var window = new MainWindow();
        try
        {
            var nav = Required<System.Windows.Controls.ListBox>(window, "NavList");
            var host = Required<ContentControl>(window, "PageHost");
            Type[] expected =
            [
                typeof(OverviewView), typeof(EdgeView), typeof(AnchorView), typeof(DotsView),
                typeof(HorizonView), typeof(RuntimeView), typeof(PresetsView)
            ];
            for (var i = 0; i < expected.Length; i++)
            {
                nav.SelectedIndex = i;
                Assert(host.Content?.GetType() == expected[i],
                    $"选择索引 {i} 后显示 {host.Content?.GetType().Name ?? "空页面"}，预期 {expected[i].Name}");
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static void PercentageSlidersUsePercentUnits()
    {
        var store = CreateStore();
        var cases = new (ConfigViewBase View, string Slider, double Expected)[]
        {
            (new EdgeView(store), "OpacitySlider", 42),
            (new AnchorView(store), "OpacitySlider", 55),
            (new DotsView(store), "DensitySlider", 55),
            (new DotsView(store), "IntensitySlider", 60),
            (new HorizonView(store), "OpacitySlider", 22)
        };

        foreach (var (view, sliderName, expected) in cases)
        {
            InvokeRefresh(view);
            var slider = Required<Slider>(view, sliderName);
            Assert(Math.Abs(slider.Value - expected) < 0.001,
                $"{view.GetType().Name}.{sliderName} 为 {slider.Value}，预期 {expected}");
        }
    }

    private static void DisabledHotkeysAreNotRegistered()
    {
        var store = CreateStore(c =>
        {
            c.OverlayEnabled = true;
            c.GameHotkeysEnabled = false;
        });
        var overlay = new OverlayManager(store);
        var hotkeys = new HotkeyManager(null, store, _ => { });
        hotkeys.Start();
        try
        {
            Assert(hotkeys.RegisteredHotkeyCount == 0,
                $"禁用后仍注册了 {hotkeys.RegisteredHotkeyCount} 个系统热键");
            hotkeys.HandleToggle("overlay", overlay);
            Assert(!store.State.OverlayEnabled, "托盘或界面的直接切换入口也被意外禁用");
        }
        finally
        {
            hotkeys.Stop();
        }
    }

    private static void UnrelatedConfigDoesNotResetBreakTimer()
    {
        var store = CreateStore(c => c.BreakEvery = BreakInterval.Every30);
        var reminder = new BreakReminder(store, new OverlayManager(store));
        reminder.Start();
        try
        {
            var before = GetPrivate<DateTime>(reminder, "_lastBreak");
            Thread.Sleep(25);
            store.Mutate(c => c.AnchorSize += 1);
            var after = GetPrivate<DateTime>(reminder, "_lastBreak");
            Assert(after == before, $"计时起点从 {before:O} 被重置为 {after:O}");
        }
        finally
        {
            reminder.Stop();
        }
    }

    private static void FrameRateChangeUpdatesExistingWindow()
    {
        var store = CreateStore(c => c.FpsPreset = FrameRatePreset.Smooth120);
        var window = new OverlayWindow(store, new MotionSource(), 0, 0, 640, 480);
        try
        {
            window.Refresh();
            var timer = GetPrivate<System.Windows.Threading.DispatcherTimer>(window, "_renderTimer");
            Assert(timer.Interval.TotalMilliseconds is > 8 and < 9,
                $"120 FPS 的计时器间隔为 {timer.Interval.TotalMilliseconds:0.##} ms");
        }
        finally
        {
            window.Close();
        }
    }

    private static void RawInputRebindsToNewWindow()
    {
        using var first = CreateMessageSource("SteadySightTestRawInput1");
        using var second = CreateMessageSource("SteadySightTestRawInput2");
        var motion = new MotionSource();
        var attach = typeof(MotionSource).GetMethod("AttachRawInput", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("找不到 AttachRawInput");

        Assert((bool)attach.Invoke(motion, [first.Handle])!, "首次 Raw Input 注册失败");
        Assert((bool)attach.Invoke(motion, [second.Handle])!, "第二次 Raw Input 注册失败");
        var target = GetPrivate<IntPtr>(motion, "_rawInputTarget");
        Assert(target == second.Handle, $"绑定目标仍为 0x{target:X}，预期 0x{second.Handle:X}");
    }

    private static HwndSource CreateMessageSource(string name)
    {
        return new HwndSource(new HwndSourceParameters(name)
        {
            Width = 1,
            Height = 1,
            WindowStyle = unchecked((int)0x80000000)
        });
    }

    private static void SingleInstanceLockIsExclusive()
    {
        var name = "SteadySight.Tests." + Guid.NewGuid().ToString("N");
        using var first = SingleInstanceGuard.TryAcquire(name);
        using var second = SingleInstanceGuard.TryAcquire(name);
        Assert(first != null, "第一个实例未取得运行锁");
        Assert(second == null, "第二个实例也取得了同名运行锁");
    }

    private static void GamepadCameraMotionUsesRightStick()
    {
        var method = typeof(MotionSource).GetMethod("ReadCameraStick", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("找不到 ReadCameraStick");
        var args = new object[] { (short)16000, (short)-12000, (short)20000, (short)10000, 0f, 0f };
        var hasInput = (bool)method.Invoke(null, args)!;
        var x = (float)args[4];
        var y = (float)args[5];

        Assert(hasInput, "右摇杆输入未被识别");
        Assert(x > 0 && y < 0, $"返回方向 ({x:0.###}, {y:0.###}) 与屏幕坐标系不符");
    }

    private static void CornerFramesTouchScreenEdges()
    {
        const double width = 1000;
        const double height = 600;
        var regions = OverlayGeometry.ComputeCornerFrames(width, height, 12);
        Assert(regions.Count == 8, $"返回了 {regions.Count} 条线段，预期 8 条");

        var touchesTop = regions.Count(r => Math.Abs(r.Y) < 0.001) == 4;
        var touchesBottom = regions.Count(r => Math.Abs(r.Y + r.Height - height) < 0.001) == 4;
        var touchesLeft = regions.Count(r => Math.Abs(r.X) < 0.001) == 4;
        var touchesRight = regions.Count(r => Math.Abs(r.X + r.Width - width) < 0.001) == 4;
        Assert(touchesTop && touchesBottom && touchesLeft && touchesRight,
            "每条屏幕边应有四条取景框线段与其贴合");
    }

    private static void AnchorRadiusTracksConfiguredSize()
    {
        Assert(Math.Abs(OverlayGeometry.AnchorRadius(6) - 3) < 0.001, "6 px 锚点半径应为 3 px");
        Assert(Math.Abs(OverlayGeometry.AnchorRadius(18) - 9) < 0.001, "18 px 锚点半径应为 9 px");
        Assert(Math.Abs(OverlayGeometry.AnchorRadius(64) - 32) < 0.001, "64 px 锚点半径应为 32 px");
    }

    private static void AutoDotsAlwaysFadeWhenIdle()
    {
        var method = typeof(OverlayRenderer).GetMethod("ShouldFadeOnIdle", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("找不到 ShouldFadeOnIdle");
        bool Invoke(DotMotionMode mode, bool setting) => (bool)method.Invoke(null, [mode, setting])!;

        Assert(Invoke(DotMotionMode.Auto, false), "智能模式被高级开关禁止淡出");
        Assert(!Invoke(DotMotionMode.MouseGamepad, false), "手动模式未尊重关闭淡出的设置");
        Assert(Invoke(DotMotionMode.Mouse, true), "手动模式未尊重开启淡出的设置");
    }

    private static void BreakNoticeCanShowOverSettings()
    {
        var method = typeof(OverlayManager).GetMethod("ShouldHideOverlay", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("找不到 ShouldHideOverlay");
        bool Invoke(bool enabled, bool ownForeground, bool breakActive) =>
            (bool)method.Invoke(null, [enabled, ownForeground, breakActive])!;

        Assert(Invoke(true, true, false), "普通叠加层未在设置窗口前台时隐藏");
        Assert(!Invoke(true, true, true), "休息提示仍被设置窗口前台状态隐藏");
        Assert(Invoke(false, false, true), "禁用叠加层时休息提示意外强制显示");
    }

    private static void StandardPresetMatchesDefaults()
    {
        var expected = new SteadyConfig();
        var actual = new SteadyConfig();
        Presets.Apply(actual, "standard");
        AssertVisualConfigEqual(expected, actual);
    }

    private static void PresetApplicationIsDeterministic()
    {
        var afterStrong = new SteadyConfig
        {
            Target = OverlayTarget.ActiveWindow,
            FpsPreset = FrameRatePreset.Smooth120,
            BreakEvery = BreakInterval.Every60
        };
        Presets.Apply(afterStrong, "strong");
        Presets.Apply(afterStrong, "standard");

        var direct = new SteadyConfig
        {
            Target = OverlayTarget.ActiveWindow,
            FpsPreset = FrameRatePreset.Smooth120,
            BreakEvery = BreakInterval.Every60
        };
        Presets.Apply(direct, "standard");

        AssertVisualConfigEqual(direct, afterStrong);
        Assert(afterStrong.Target == OverlayTarget.ActiveWindow, "预设意外重置了叠加目标");
        Assert(afterStrong.FpsPreset == FrameRatePreset.Smooth120, "预设意外重置了帧率");
        Assert(afterStrong.BreakEvery == BreakInterval.Every60, "预设意外重置了休息间隔");
    }

    private static void AssertVisualConfigEqual(SteadyConfig expected, SteadyConfig actual)
    {
        var properties = typeof(SteadyConfig).GetProperties()
            .Where(p => p.Name is not nameof(SteadyConfig.OverlayEnabled)
                and not nameof(SteadyConfig.Target)
                and not nameof(SteadyConfig.FpsPreset)
                and not nameof(SteadyConfig.GameHotkeysEnabled)
                and not nameof(SteadyConfig.BreakEvery)
                and not nameof(SteadyConfig.Language)
                and not nameof(SteadyConfig.ProfileName));
        foreach (var property in properties)
        {
            var expectedValue = property.GetValue(expected);
            var actualValue = property.GetValue(actual);
            Assert(Equals(expectedValue, actualValue),
                $"{property.Name} 为 {actualValue}，预期 {expectedValue}");
        }
    }

    private static void CapturePages(string output)
    {
        Directory.CreateDirectory(output);
        SetAppStore(CreateStore());
        var window = new MainWindow
        {
            Width = 1100,
            Height = 720,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -2000,
            Top = 0
        };
        window.Show();
        window.UpdateLayout();

        var nav = Required<System.Windows.Controls.ListBox>(window, "NavList");
        for (var i = 0; i < 7; i++)
        {
            nav.SelectedIndex = i;
            window.UpdateLayout();
            var dpi = VisualTreeHelper.GetDpi(window);
            var width = Math.Max(1, (int)Math.Ceiling(window.ActualWidth * dpi.DpiScaleX));
            var height = Math.Max(1, (int)Math.Ceiling(window.ActualHeight * dpi.DpiScaleY));
            var bitmap = new RenderTargetBitmap(width, height, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            bitmap.Render(window);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(Path.Combine(output, $"page-{i + 1}.png"));
            encoder.Save(stream);
        }
        window.Close();
        Console.WriteLine($"已生成 7 张页面截图：{output}");
    }

    private static ConfigStore CreateStore(Action<SteadyConfig>? edit = null)
    {
        var config = new SteadyConfig();
        edit?.Invoke(config);
        var path = Path.Combine(Path.GetTempPath(), "SteadySight.Tests", Guid.NewGuid() + ".json");
        return new ConfigStore(config, path);
    }

    private static void SetAppStore(ConfigStore store)
    {
        var property = typeof(App).GetProperty(nameof(App.Store), BindingFlags.Public | BindingFlags.Static);
        property?.SetValue(null, store);
    }

    private static void InvokeRefresh(ConfigViewBase view)
    {
        var method = view.GetType().GetMethod("RefreshFromConfig", BindingFlags.Instance | BindingFlags.NonPublic);
        method?.Invoke(view, null);
    }

    private static T Required<T>(FrameworkElement element, string name) where T : class
    {
        return element.FindName(name) as T
            ?? throw new InvalidOperationException($"找不到控件 {element.GetType().Name}.{name}");
    }

    private static T GetPrivate<T>(object instance, string name)
    {
        var field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"找不到字段 {name}");
        return (T)field.GetValue(instance)!;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
