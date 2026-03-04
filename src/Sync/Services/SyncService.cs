using ActivitiesTracker.Domain.Contracts;
using ActivitiesTracker.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace ActivitiesTracker.Sync.Services;

public sealed class SyncService(ILogSink logSink) : Contracts.ISyncService
{
    public async Task<int> QueuePendingChangeAsync(string sourceId, string tabName, string operation, string recordId, string payloadJson, CancellationToken cancellationToken = default)
    {
        var queueId = Guid.NewGuid().ToString();
        await using var connection = new SqliteConnection(SqliteBootstrapper.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"INSERT INTO pending_changes
(queue_id, source_id, tab_name, operation, record_id, payload_json, created_at, status)
VALUES ($queueId, $sourceId, $tabName, $operation, $recordId, $payloadJson, $createdAt, 'pending');";

        cmd.Parameters.AddWithValue("$queueId", queueId);
        cmd.Parameters.AddWithValue("$sourceId", sourceId);
        cmd.Parameters.AddWithValue("$tabName", tabName);
        cmd.Parameters.AddWithValue("$operation", operation);
        cmd.Parameters.AddWithValue("$recordId", recordId);
        cmd.Parameters.AddWithValue("$payloadJson", payloadJson);
        cmd.Parameters.AddWithValue("$createdAt", DateTimeOffset.UtcNow.ToString("o"));

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        logSink.Info($"Queued change {queueId} for {tabName}/{operation}/{recordId}");
        return rows;
    }

    public async Task<int> SyncNowAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(SqliteBootstrapper.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(1) FROM pending_changes WHERE status = 'pending';";
        var pending = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        // V1 scaffold: marks pending changes as applied locally.
        await using var updateCommand = connection.CreateCommand();
        updateCommand.CommandText = "UPDATE pending_changes SET status = 'applied' WHERE status = 'pending';";
        await updateCommand.ExecuteNonQueryAsync(cancellationToken);

        logSink.Info($"SyncNow processed {pending} queued item(s).");
        return pending;
    }
}
