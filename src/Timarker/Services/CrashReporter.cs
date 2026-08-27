using System.Text;

namespace Timarker.Services;

internal static class CrashReporter
{
    private static int _messageShown;

    public static void Initialize()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Report(e.Exception, showMessage: true);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Report(e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()), showMessage: false);
        TaskScheduler.UnobservedTaskException += (_, e) => { Report(e.Exception, showMessage: false); e.SetObserved(); };
    }

    private static void Report(Exception exception, bool showMessage)
    {
        try
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Timarker", "Logs");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"crash-{DateTime.Now:yyyyMMdd}.log");
            var text = new StringBuilder()
                .AppendLine(new string('=', 72))
                .AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"))
                .AppendLine($"Timarker {Application.ProductVersion}")
                .AppendLine($"Windows {Environment.OSVersion} · .NET {Environment.Version}")
                .AppendLine(exception.ToString())
                .ToString();
            File.AppendAllText(path, text);
            if (showMessage && Interlocked.Exchange(ref _messageShown, 1) == 0)
                MessageBox.Show($"Timarker 遇到了问题，错误日志已保存。\n\n{path}\n\n重新打开应用后，数据会从最近一次有效保存中恢复。", "Timarker 发生错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch
        {
            // 错误报告自身不能再次导致应用崩溃。
        }
    }
}
