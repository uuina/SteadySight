# SteadySight 稳视

SteadySight 是面向 Windows 桌面 3D 游戏的视觉辅助叠加层。它提供边缘遮罩、中心锚点、实验性动态圆点、水平参考线、预设、全局热键和休息提醒。

## 当前状态

- 版本：1.0.0
- 平台：Windows 10/11 x64
- 技术：.NET 8、WPF、Win32 Raw Input、XInput
- 交付形式：免安装自包含单文件程序
- 推荐游戏模式：窗口化或无边框窗口

独占全屏游戏、带有反作弊或禁止第三方叠加层的游戏可能无法显示叠加层。请遵守游戏服务条款。

## 使用

1. 运行 `SteadySight.exe`。
2. 选择一个预设，再按自身感受逐步调整强度。
3. 关闭主窗口后程序留在系统托盘；通过托盘菜单退出。

全局快捷键：

| 按键 | 功能 |
| --- | --- |
| F1 | 叠加层总开关 |
| F2 | 循环边缘样式 |
| F3 | 循环中心锚点 |
| F4 | 循环动态圆点模式 |
| Ctrl+F5 | 循环预设 |
| Ctrl+F6 | 显示休息提示 |

关闭“启用游戏内全局快捷键”后，程序会注销这些系统热键，不再占用游戏按键。

配置保存在 `%APPDATA%\SteadySight\settings.json`。使用 `SteadySight.exe --reset` 可恢复默认配置。

## 开发与验证

需要 .NET 8 SDK。PowerShell 7.1 及以上：

```powershell
dotnet build .\src\SteadySight\SteadySight.csproj -c Release
dotnet run --project .\tests\SteadySight.Tests\SteadySight.Tests.csproj -c Release
```

发布免安装单文件：

```powershell
dotnet publish .\src\SteadySight\SteadySight.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\release\SteadySight-v1.0.0-win-x64
```

测试程序不依赖第三方测试框架，运行后返回非零退出码表示存在失败用例。

## 适用边界

SteadySight 不是医疗器械，也不替代医疗建议。项目借鉴了头显 VR 中关于视野限制与静态视觉参照的研究，但这些研究结果并不一致，也不能直接证明桌面 3D 游戏中的效果。当前版本尚未完成用户对照试验。

出现眩晕、恶心、头痛或视疲劳时，应立即停止游戏并休息；症状持续或严重时请就医。

## 已知限制

- 仅检测第一只 XInput 手柄的右摇杆。
- 动态圆点依据鼠标/手柄输入估计视角运动，无法读取游戏内部摄像机状态。
- 程序当前未做代码签名，Windows 可能显示未知发布者提示。
- 休息提示依赖叠加层窗口；叠加层关闭时不会显示覆盖提示。
