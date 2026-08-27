# Timarker 插件

Timarker 插件是受信任的本机 .NET 代码。插件与 Timarker 运行在同一进程中，因此只应安装来源可信的插件。

## 插件包

插件包扩展名可以是 `.tmplugin` 或 `.zip`，根目录必须包含：

```text
plugin.json
MyPlugin.dll
依赖程序集（如有）
```

`plugin.json` 示例：

```json
{
  "id": "example-focus-report",
  "name": "专注报告",
  "version": "1.0.0",
  "description": "查看自定义的专注统计。",
  "entryAssembly": "MyPlugin.dll",
  "entryType": "MyPlugin.FocusReportPlugin"
}
```

入口类型需实现 Timarker 程序集公开的 `ITimarkerPlugin`：

```csharp
public sealed class FocusReportPlugin : ITimarkerPlugin
{
    public Control CreateView() => new MyPluginView();
    public void Dispose() { }
}
```

插件项目使用 `net8.0-windows`、启用 Windows Forms，并引用与目标 Timarker 版本一致的 `Timarker.dll`。`CreateView` 每次被打开时应返回一个新的控件。

## 安装位置

插件会被解压到 `%LocalAppData%\Timarker\Plugins\<插件Id>`。启用、停用、安装或卸载后保存设置，Timarker 会重启并重新加载插件。
