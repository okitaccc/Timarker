using System.IO.Compression;
using Timarker.SamplePlugin;
using Xunit;

namespace Timarker.Plugin.Tests;

public sealed class PluginManagerTests
{
    [Fact]
    public void InstallsLoadsAndCreatesPluginView()
    {
        var root = Path.Combine(Path.GetTempPath(), "timarker-plugin-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var package = Path.Combine(root, "sample.tmplugin");
        try
        {
            using (var archive = ZipFile.Open(package, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(typeof(HelloPlugin).Assembly.Location, "Timarker.SamplePlugin.dll");
                var manifest = archive.CreateEntry("plugin.json");
                using var writer = new StreamWriter(manifest.Open());
                writer.Write("""{"id":"sample-hello","name":"示例插件","version":"1.0.0","description":"测试","entryAssembly":"Timarker.SamplePlugin.dll","entryType":"Timarker.SamplePlugin.HelloPlugin"}""");
            }

            using (var manager = new PluginManager(Path.Combine(root, "plugins")))
            {
                manager.Install(package);
                manager.LoadEnabled();
                using var view = manager.CreateView("sample-hello");
                Assert.Contains("Timarker 插件页面", view.Text);
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
