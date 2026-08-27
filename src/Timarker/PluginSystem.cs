using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace Timarker;

public interface ITimarkerPlugin : IDisposable
{
    Control CreateView();
}

public sealed class PluginManifest
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = "";
    public string EntryAssembly { get; set; } = "";
    public string EntryType { get; set; } = "";
}

public sealed class PluginDescriptor
{
    internal PluginDescriptor(PluginManifest manifest, string directory)
    {
        Manifest = manifest;
        Directory = directory;
    }

    public PluginManifest Manifest { get; }
    public string Directory { get; }
    public string? Error { get; internal set; }
    internal PluginLoadContext? LoadContext { get; set; }
    internal ITimarkerPlugin? Instance { get; set; }
}

public sealed class PluginManager : IDisposable
{
    private readonly string _directory;
    private readonly HashSet<string> _disabled;
    private readonly List<PluginDescriptor> _plugins = [];
    public IReadOnlyList<PluginDescriptor> Plugins => _plugins;

    public PluginManager(string directory, IEnumerable<string>? disabled = null)
    {
        _directory = directory;
        _disabled = new HashSet<string>(disabled ?? [], StringComparer.OrdinalIgnoreCase);
        Directory.CreateDirectory(_directory);
        Scan();
    }

    public bool IsEnabled(string id) => !_disabled.Contains(id);

    public void Scan()
    {
        _plugins.Clear();
        foreach (var manifestPath in Directory.EnumerateFiles(_directory, "plugin.json", SearchOption.AllDirectories))
        {
            try
            {
                var manifest = JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(manifestPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidDataException("插件清单为空。");
                Validate(manifest);
                if (_plugins.Any(x => x.Manifest.Id.Equals(manifest.Id, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException($"插件标识重复：{manifest.Id}");
                _plugins.Add(new PluginDescriptor(manifest, Path.GetDirectoryName(manifestPath)!));
            }
            catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException)
            {
                var manifestDirectory = Path.GetDirectoryName(manifestPath)!;
                _plugins.Add(new PluginDescriptor(new PluginManifest { Id = Path.GetFileName(manifestDirectory), Name = "无法读取的插件" }, manifestDirectory) { Error = ex.Message });
            }
        }
    }

    public void LoadEnabled()
    {
        foreach (var plugin in _plugins.Where(x => IsEnabled(x.Manifest.Id) && x.Error is null))
        {
            try
            {
                var assemblyPath = Path.GetFullPath(Path.Combine(plugin.Directory, plugin.Manifest.EntryAssembly));
                if (!assemblyPath.StartsWith(Path.GetFullPath(plugin.Directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("入口程序集必须位于插件目录内。");
                var context = new PluginLoadContext(assemblyPath);
                var assembly = context.LoadEntryAssembly(assemblyPath);
                var type = assembly.GetType(plugin.Manifest.EntryType, true)!;
                plugin.Instance = Activator.CreateInstance(type) as ITimarkerPlugin
                    ?? throw new InvalidDataException("入口类型没有实现 ITimarkerPlugin。");
                plugin.LoadContext = context;
            }
            catch (Exception ex)
            {
                plugin.Error = ex.GetBaseException().Message;
            }
        }
    }

    public PluginDescriptor Install(string packagePath)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        var manifestEntry = archive.Entries.FirstOrDefault(x => x.FullName.Equals("plugin.json", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException("插件包根目录缺少 plugin.json。");
        PluginManifest manifest;
        using (var stream = manifestEntry.Open())
            manifest = JsonSerializer.Deserialize<PluginManifest>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidDataException("插件清单为空。");
        Validate(manifest);
        var target = Path.Combine(_directory, manifest.Id);
        if (Directory.Exists(target)) throw new InvalidOperationException("同名插件已经安装。");
        Directory.CreateDirectory(target);
        try
        {
            foreach (var entry in archive.Entries)
            {
                var destination = Path.GetFullPath(Path.Combine(target, entry.FullName));
                if (!destination.StartsWith(Path.GetFullPath(target) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("插件包包含不安全的路径。");
                if (string.IsNullOrEmpty(entry.Name)) Directory.CreateDirectory(destination);
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    entry.ExtractToFile(destination);
                }
            }
        }
        catch
        {
            Directory.Delete(target, true);
            throw;
        }
        Scan();
        return _plugins.Single(x => x.Manifest.Id.Equals(manifest.Id, StringComparison.OrdinalIgnoreCase));
    }

    public void Uninstall(PluginDescriptor plugin)
    {
        if (plugin.Instance is not null) throw new InvalidOperationException("请先停用插件并重启 Timarker，再执行卸载。");
        Directory.Delete(plugin.Directory, true);
        _plugins.Remove(plugin);
    }

    public Control CreateView(string id)
    {
        var plugin = _plugins.FirstOrDefault(x => x.Manifest.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (plugin?.Instance is null) throw new InvalidOperationException(plugin?.Error ?? "插件未加载。");
        return plugin.Instance.CreateView();
    }

    public void Dispose()
    {
        foreach (var plugin in _plugins)
        {
            plugin.Instance?.Dispose();
            plugin.Instance = null;
            plugin.LoadContext?.Unload();
        }
    }

    private static void Validate(PluginManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.Id) || manifest.Id.Any(x => !char.IsAsciiLetterOrDigit(x) && x is not '-' and not '_'))
            throw new InvalidDataException("插件 Id 只能包含英文字母、数字、短横线和下划线。");
        if (string.IsNullOrWhiteSpace(manifest.Name) || string.IsNullOrWhiteSpace(manifest.EntryAssembly) || string.IsNullOrWhiteSpace(manifest.EntryType))
            throw new InvalidDataException("插件清单缺少名称、入口程序集或入口类型。");
    }
}

internal sealed class PluginLoadContext(string pluginPath) : AssemblyLoadContext(isCollectible: true)
{
    private readonly AssemblyDependencyResolver _resolver = new(pluginPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name == typeof(ITimarkerPlugin).Assembly.GetName().Name) return null;
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }

    public Assembly LoadEntryAssembly(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }
}
