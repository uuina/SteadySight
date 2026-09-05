using System.IO;
using System.Text.Json;
using System.Windows.Threading;
using SteadySight.Models;

namespace SteadySight.Services;

/// <summary>线程安全配置访问：UI 线程写，渲染线程读。</summary>
public sealed class ConfigStore
{
    private readonly object _gate = new();
    private readonly string _path;
    private readonly DispatcherTimer _saveTimer;

    public SteadyConfig State { get; private set; }
    public event Action? Changed;

    public ConfigStore(SteadyConfig initial, string path)
    {
        State = initial;
        _path = path;
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); Save(); };
    }

    public static SteadyConfig Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var cfg = JsonSerializer.Deserialize<SteadyConfig>(json);
                if (cfg != null)
                {
                    Clamp(cfg);
                    return cfg;
                }
            }
        }
        catch
        {
            // 损坏配置回退默认
        }
        var fresh = new SteadyConfig();
        Clamp(fresh);
        return fresh;
    }

    /// <summary>修改配置（UI 线程调用），钳制范围并节流保存。</summary>
    public void Mutate(Action<SteadyConfig> edit)
    {
        lock (_gate)
        {
            edit(State);
            Clamp(State);
        }
        Changed?.Invoke();
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    public void ReplaceProfile(SteadyConfig profile)
    {
        lock (_gate)
        {
            State = profile;
            Clamp(State);
        }
        Changed?.Invoke();
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    public void Save()
    {
        lock (_gate)
        {
            try
            {
                var dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_path, JsonSerializer.Serialize(State,
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { /* 保存失败不中断运行 */ }
        }
    }

    private static void Clamp(SteadyConfig c)
    {
        c.EdgeWidthPercent = Math.Clamp(c.EdgeWidthPercent, 4, 38);
        c.EdgeOpacity = Math.Clamp(c.EdgeOpacity, 0.05, 0.85);
        c.AnchorSize = Math.Clamp(c.AnchorSize, 6, 64);
        c.AnchorOpacity = Math.Clamp(c.AnchorOpacity, 0.1, 1);
        c.DotDensity = Math.Clamp(c.DotDensity, 0.1, 1);
        c.DotSize = Math.Clamp(c.DotSize, 1, 8);
        c.DotIntensity = Math.Clamp(c.DotIntensity, 0.1, 1.2);
        c.DotMaxOpacity = Math.Clamp(c.DotMaxOpacity, 0.1, 0.9);
        c.HorizonOpacity = Math.Clamp(c.HorizonOpacity, 0.05, 0.6);
        c.HorizonOffsetPercent = Math.Clamp(c.HorizonOffsetPercent, -25, 25);
    }
}
