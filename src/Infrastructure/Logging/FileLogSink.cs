using ActivitiesTracker.Domain.Contracts;
using ActivitiesTracker.Infrastructure.Config;

namespace ActivitiesTracker.Infrastructure.Logging;

public sealed class FileLogSink : ILogSink
{
    private readonly string _logFilePath;
    private readonly object _sync = new();

    public FileLogSink()
    {
        Directory.CreateDirectory(AppPaths.LogsDirectory);
        _logFilePath = Path.Combine(AppPaths.LogsDirectory, $"app-{DateTime.UtcNow:yyyyMMdd}.log");
    }

    public void Info(string message) => Write("INFO", message);

    public void Error(string message, Exception? exception = null)
    {
        var finalMessage = exception is null ? message : $"{message}{Environment.NewLine}{exception}";
        Write("ERROR", finalMessage);
    }

    private void Write(string level, string message)
    {
        lock (_sync)
        {
            File.AppendAllText(_logFilePath, $"{DateTimeOffset.UtcNow:o} [{level}] {message}{Environment.NewLine}");
        }
    }
}
