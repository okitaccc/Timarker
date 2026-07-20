namespace Timarker;

internal static class AppPaths
{
    public static string DataDirectory
    {
        get
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var directory = Path.Combine(localAppData, "Timarker");
            var legacyDirectory = Path.Combine(localAppData, "Timeline");
            if (!Directory.Exists(directory) && Directory.Exists(legacyDirectory))
            {
                Directory.Move(legacyDirectory, directory);
            }
            Directory.CreateDirectory(directory);
            return directory;
        }
    }
}
