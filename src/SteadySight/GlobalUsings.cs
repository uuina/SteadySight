// 统一别名：项目同时启用 WPF 与 WinForms，SDK 会隐式注入
// System.Windows.Forms / System.Drawing，此处收窄常见类型的名称解析。
global using Application = System.Windows.Application;
global using Window = System.Windows.Window;
global using UserControl = System.Windows.Controls.UserControl;
global using ComboBox = System.Windows.Controls.ComboBox;
global using Slider = System.Windows.Controls.Slider;
global using TextBlock = System.Windows.Controls.TextBlock;
global using CheckBox = System.Windows.Controls.CheckBox;
global using Button = System.Windows.Controls.Button;
global using TextBox = System.Windows.Controls.TextBox;
global using ListBox = System.Windows.Controls.ListBox;
global using ContentControl = System.Windows.Controls.ContentControl;
global using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;
global using Color = System.Windows.Media.Color;
global using Pen = System.Windows.Media.Pen;
global using Brushes = System.Windows.Media.Brushes;
global using Point = System.Windows.Point;
global using FlowDirection = System.Windows.FlowDirection;
global using ColorConverter = System.Windows.Media.ColorConverter;
