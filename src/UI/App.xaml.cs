using System.Windows;
using ActivitiesTracker.Infrastructure.Data;
using ActivitiesTracker.Infrastructure.Logging;

namespace ActivitiesTracker.UI;

public partial class App : Application
{
    private readonly FileLogSink _logSink = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            _logSink.Error("Unhandled UI exception", args.Exception);
            MessageBox.Show($"Unexpected error: {args.Exception.Message}", "ActivitiesTracker", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            SqliteBootstrapper.EnsureCreated();
            _logSink.Info("Application startup complete.");
        }
        catch (Exception ex)
        {
            _logSink.Error("Startup failed while initializing local storage", ex);
            MessageBox.Show($"Cannot initialize local storage.\n{ex.Message}", "ActivitiesTracker", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
            return;
        }

        base.OnStartup(e);
    }
}
