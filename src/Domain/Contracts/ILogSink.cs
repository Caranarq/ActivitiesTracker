namespace ActivitiesTracker.Domain.Contracts;

public interface ILogSink
{
    void Info(string message);
    void Error(string message, Exception? exception = null);
}
