namespace Holgura.Platform.Windows;

public static class WindowsPaths
{
    public static string LocalAppData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public static string RoamingAppData => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    public static string Windows => Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    public static string UserProfile => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static string AppDataDirectory => Path.Combine(LocalAppData, "Holgura");
}
