using Timarker;

namespace Timarker.SamplePlugin;

public sealed class HelloPlugin : ITimarkerPlugin
{
    public Control CreateView() => new Label
    {
        Text = "你好，这是一个 Timarker 插件页面。",
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Microsoft YaHei UI", 16, FontStyle.Bold)
    };

    public void Dispose() { }
}
