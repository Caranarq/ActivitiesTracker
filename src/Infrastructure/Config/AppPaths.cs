namespace ActivitiesTracker.Infrastructure.Config;

public static class AppPaths
{
    public static string AppDataRoot
    {
        get
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ActivitiesTracker");
            Directory.CreateDirectory(root);
            return root;
        }
    }

    public static string DatabasePath => Path.Combine(AppDataRoot, "activities_tracker.db");
    public static string LogsDirectory => Path.Combine(AppDataRoot, "logs");

    public static string CredentialsPath => Path.Combine(AppDataRoot, "credentials.json");
}
