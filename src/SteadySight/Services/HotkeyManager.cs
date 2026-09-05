using System.Runtime.InteropServices;
using System.Windows.Interop;
using SteadySight.Models;

namespace SteadySight.Services;

public sealed class HotkeyManager
{
    public const int BaseId = 0x4100;

    private readonly ConfigStore _store;
    private readonly Action<string> _toggleAction;
    private HwndSource? _source;
    private int _nextId = BaseId;
    private readonly HashSet<int> _registeredIds = new();
    private bool _hotkeysEnabled;

    public int RegisteredHotkeyCount => _registeredIds.Count;

    public HotkeyManager(Window? owner, ConfigStore store, Action<string> toggleAction)
    {
        _store = store;
        _toggleAction = toggleAction;
    }

    public void Start()
    {
        if (_source != null) return;
        _source = CreateMessageWindow();
        _source.AddHook(WndProc);
        _hotkeysEnabled = _store.State.GameHotkeysEnabled;
        _store.Changed += OnConfigChanged;
        if (_hotkeysEnabled) RegisterAll();
    }

    private HwndSource CreateMessageWindow()
    {
        var p = new HwndSourceParameters("SteadySightHotkey")
        {
            Width = 0,
            Height = 0,
            WindowStyle = unchecked((int)0x80000000), // WS_POPUP
            UsesPerPixelOpacity = true
        };
        var src = new HwndSource(p);
        return src;
    }

    public void Stop()
    {
        if (_source == null) return;
        _store.Changed -= OnConfigChanged;
        UnregisterAll();
        _source.RemoveHook(WndProc);
        _source.Dispose();
        _source = null;
    }

    private void RegisterAll()
    {
        if (_source == null) return;
        // F1 总开关, F2 边缘, F3 锚点, F4 动态点, F5 预设循环, F6 休息提示
        Register(Win32Native.ModNoRepeat, 0x70); // F1
        Register(Win32Native.ModNoRepeat, 0x71);
        Register(Win32Native.ModNoRepeat, 0x72);
        Register(Win32Native.ModNoRepeat, 0x73);
        Register(Win32Native.ModNoRepeat | 0x0002, 0x74); // Ctrl+F5
        Register(Win32Native.ModNoRepeat | 0x0002, 0x75); // Ctrl+F6
    }

    private void Register(uint mods, uint vk) => RegisterHotKey(mods, vk);

    private void RegisterHotKey(uint mods, uint vk)
    {
        if (_source == null) return;
        if (Win32Native.RegisterHotKey(_source.Handle, _nextId, mods, vk))
            _registeredIds.Add(_nextId);
        _nextId++;
    }

    private void UnregisterAll()
    {
        if (_source == null) return;
        foreach (var id in _registeredIds)
            Win32Native.UnregisterHotKey(_source.Handle, id);
        _registeredIds.Clear();
        _nextId = BaseId;
    }

    private void OnConfigChanged()
    {
        var enabled = _store.State.GameHotkeysEnabled;
        if (enabled == _hotkeysEnabled) return;
        _hotkeysEnabled = enabled;
        UnregisterAll();
        if (enabled) RegisterAll();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32Native.WmHotkey)
        {
            var id = wParam.ToInt32();
            var idx = id - BaseId;
            string key = idx switch
            {
                0 => "overlay",
                1 => "edge",
                2 => "anchor",
                3 => "dots",
                4 => "preset",
                5 => "break",
                _ => ""
            };
            if (!string.IsNullOrEmpty(key)) _toggleAction(key);
            handled = true;
            return IntPtr.Zero;
        }
        return IntPtr.Zero;
    }

    public void HandleToggle(string key, OverlayManager overlay)
    {
        switch (key)
        {
            case "overlay":
                overlay.Toggle();
                break;
            case "edge":
                _store.Mutate(c => c.EdgeStyle = NextEnum(c.EdgeStyle));
                break;
            case "anchor":
                _store.Mutate(c => c.AnchorStyle = NextEnum(c.AnchorStyle));
                break;
            case "dots":
                _store.Mutate(c => c.DotMode = NextEnum(c.DotMode));
                break;
            case "preset":
                RotatePreset();
                break;
            case "break":
                overlay.ShowBreakNotice(15);
                break;
        }
    }

    private static T NextEnum<T>(T value) where T : struct, Enum
    {
        var values = (T[])Enum.GetValues(typeof(T));
        var idx = Array.IndexOf(values, value);
        return values[(idx + 1) % values.Length];
    }

    private void RotatePreset()
    {
        var current = _store.State.ProfileName;
        var def = Presets.All.FirstOrDefault(p => p.ZhName == current) ?? Presets.All[0];
        var idx = Array.IndexOf(Presets.All, def);
        var next = Presets.All[(idx + 1) % Presets.All.Length];
        _store.Mutate(c => Presets.Apply(c, next.Key));
    }
}
