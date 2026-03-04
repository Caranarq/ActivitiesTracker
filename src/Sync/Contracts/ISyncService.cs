namespace ActivitiesTracker.Sync.Contracts;

public interface ISyncService
{
    Task<int> QueuePendingChangeAsync(string sourceId, string tabName, string operation, string recordId, string payloadJson, CancellationToken cancellationToken = default);
    Task<int> SyncNowAsync(CancellationToken cancellationToken = default);
}
