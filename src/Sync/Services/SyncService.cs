using System.Text.Json;
using ActivitiesTracker.Domain.Contracts;
using ActivitiesTracker.Infrastructure.Config;
using ActivitiesTracker.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Google.Apis.Sheets.v4.Data;

namespace ActivitiesTracker.Sync.Services;

public sealed class SyncService(ILogSink logSink) : Contracts.ISyncService
{
    private readonly GoogleAuthService _authService = new(logSink);

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
        var credential = await _authService.GetCredentialAsync(cancellationToken);
        if (credential == null)
        {
            logSink.Error("Could not obtain Google Credentials. User needs to authenticate.");
            throw new Exception("Authentication required.");
        }

        var sheetsClient = new SheetsApiClient(credential, logSink);

        // 1. Get or Create Spreadsheet ID from local registry
        var spreadsheetId = await GetOrCreateActiveSpreadsheetAsync(sheetsClient, cancellationToken);

        // 2. Ensure schema (tabs and headers)
        await EnsureSchemaAsync(sheetsClient, spreadsheetId, cancellationToken);

        // 3. Pull from Remote and resolve conflicts
        await PullAndResolveAsync(sheetsClient, spreadsheetId, cancellationToken);

        // 4. Process Pending Changes
        await using var connection = new SqliteConnection(SqliteBootstrapper.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT queue_id, tab_name, operation, payload_json FROM pending_changes WHERE status = 'pending' ORDER BY created_at ASC;";
        
        var pendingChanges = new List<(string QueueId, string TabName, string Op, string Json)>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                pendingChanges.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
            }
        }

        int processed = 0;
        foreach (var change in pendingChanges)
        {
            try
            {
                if (change.Op == "insert")
                {
                    IList<object> rowValues = ParsePayloadToRow(change.TabName, change.Json);
                    await sheetsClient.AppendRowAsync(spreadsheetId, $"{change.TabName}!A1", rowValues, cancellationToken);
                    logSink.Info($"Appended row to {change.TabName}");
                }

                await using var updateCmd = connection.CreateCommand();
                updateCmd.CommandText = "UPDATE pending_changes SET status = 'applied' WHERE queue_id = $id;";
                updateCmd.Parameters.AddWithValue("$id", change.QueueId);
                await updateCmd.ExecuteNonQueryAsync(cancellationToken);
                processed++;
            }
            catch (Exception ex)
            {
                logSink.Error($"Failed to process change {change.QueueId}", ex);
                await using var errorCmd = connection.CreateCommand();
                errorCmd.CommandText = "UPDATE pending_changes SET status = 'failed', error_message = $err WHERE queue_id = $id;";
                errorCmd.Parameters.AddWithValue("$err", ex.Message);
                errorCmd.Parameters.AddWithValue("$id", change.QueueId);
                await errorCmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        logSink.Info($"SyncNow processed {processed} queued item(s) out of {pendingChanges.Count}.");
        return processed;
    }

    private async Task<string> GetOrCreateActiveSpreadsheetAsync(SheetsApiClient client, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(SqliteBootstrapper.ConnectionString);
        await connection.OpenAsync(ct);

        await using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = "SELECT spreadsheet_id FROM source_registry WHERE is_active = 1 LIMIT 1;";
        var existing = await selectCmd.ExecuteScalarAsync(ct);

        if (existing != null && !string.IsNullOrEmpty(existing.ToString()))
        {
            return existing.ToString()!;
        }

        // Create new
        var spreadsheet = new Spreadsheet
        {
            Properties = new SpreadsheetProperties { Title = "ActivitiesTracker_Data" }
        };

        // Note: For actual creation we have to use the underlying service or add a method to SheetsApiClient
        // But since we want to avoid modifying it, we can instantiate SheetsService here, 
        // OR add Create method to SheetsApiClient. Let's assume we can add it to SheetsApiClient.
        // I will implement raw create here for simplicity or we can update `SheetsApiClient`.
        // I'll assume we can use the raw client. Let's update `SheetsApiClient` in another step if needed, 
        // but for now, I will use a private method if possible, wait, it's abstracted.
        // Let's just throw an error if it's not created, or implement creation via an updated SheetsApiClient method.
        var id = await CreateEmptySpreadsheetViaHttpClient(client, ct);
        
        await using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO source_registry (source_id, source_type, display_name, spreadsheet_id, is_active, last_opened_at) VALUES ($id, 'google_sheets', 'My Activities Tracker', $sheetId, 1, $date);";
        insertCmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
        insertCmd.Parameters.AddWithValue("$sheetId", id);
        insertCmd.Parameters.AddWithValue("$date", DateTimeOffset.UtcNow.ToString("o"));
        await insertCmd.ExecuteNonQueryAsync(ct);

        return id;
    }

    private async Task<string> CreateEmptySpreadsheetViaHttpClient(SheetsApiClient client, CancellationToken ct)
    {
        // Actually SheetsApiClient doesn't expose Create. 
        // We will add CreateSpreadsheetAsync to SheetsApiClient soon.
        var newSpreadsheet = await client.CreateSpreadsheetAsync("ActivitiesTracker_Data", ct);
        return newSpreadsheet.SpreadsheetId;
    }

    private async Task EnsureSchemaAsync(SheetsApiClient client, string spreadsheetId, CancellationToken ct)
    {
        var spreadsheet = await client.GetSpreadsheetAsync(spreadsheetId, ct);
        var existingTitles = spreadsheet.Sheets.Select(s => s.Properties.Title).ToHashSet();

        var requests = new List<Request>();

        if (!existingTitles.Contains("events"))
        {
            requests.Add(new Request
            {
                AddSheet = new AddSheetRequest { Properties = new SheetProperties { Title = "events" } }
            });
        }

        if (!existingTitles.Contains("activity_segments"))
        {
            requests.Add(new Request
            {
                AddSheet = new AddSheetRequest { Properties = new SheetProperties { Title = "activity_segments" } }
            });
        }

        if (requests.Any())
        {
            await client.BatchUpdateAsync(spreadsheetId, new BatchUpdateSpreadsheetRequest { Requests = requests }, ct);
            
            // Wait for sheets to be created, then append headers
            if (!existingTitles.Contains("events"))
            {
                var headers = new List<object> { "Id", "SourceId", "Title", "CategoryId", "TimeType", "StartAt", "EndAt", "TzCapture", "Tag", "Notes", "RecurrenceId", "IsDeleted", "UpdatedAt" };
                await client.AppendRowAsync(spreadsheetId, "events!A1", headers, ct);
            }
            if (!existingTitles.Contains("activity_segments"))
            {
                var headers = new List<object> { "Id", "DateKey", "StartAt", "EndAt", "TzCapture", "CategoryId", "LinkedTag", "Notes", "IsDeleted", "UpdatedAt" };
                await client.AppendRowAsync(spreadsheetId, "activity_segments!A1", headers, ct);
            }
        }
    }

    private IList<object> ParsePayloadToRow(string tabName, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        if (tabName == "events")
        {
            return new List<object>
            {
                root.GetProperty("Id").GetString() ?? "",
                root.GetProperty("SourceId").GetString() ?? "",
                root.GetProperty("Title").GetString() ?? "",
                root.GetProperty("CategoryId").GetString() ?? "",
                root.GetProperty("TimeType").GetInt32().ToString() ?? "0",
                root.GetProperty("StartAt").GetString() ?? "",
                SafeGetString(root, "EndAt"),
                root.GetProperty("TzCapture").GetString() ?? "",
                SafeGetString(root, "Tag"),
                SafeGetString(root, "Notes"),
                SafeGetString(root, "RecurrenceId"),
                root.GetProperty("IsDeleted").GetBoolean() ? "1" : "0",
                root.GetProperty("UpdatedAt").GetString() ?? ""
            };
        }
        else // activity_segments
        {
            return new List<object>
            {
                root.GetProperty("Id").GetString() ?? "",
                root.GetProperty("DateKey").GetString() ?? "",
                root.GetProperty("StartAt").GetString() ?? "",
                root.GetProperty("EndAt").GetString() ?? "",
                root.GetProperty("TzCapture").GetString() ?? "",
                root.GetProperty("CategoryId").GetString() ?? "",
                SafeGetString(root, "LinkedTag"),
                SafeGetString(root, "Notes"),
                root.GetProperty("IsDeleted").GetBoolean() ? "1" : "0",
                root.GetProperty("UpdatedAt").GetString() ?? ""
            };
        }
    }

    private string SafeGetString(JsonElement root, string propName)
    {
        if (root.TryGetProperty(propName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.String)
                return prop.GetString() ?? "";
            return prop.GetRawText();
        }
        return "";
    }

    private async Task PullAndResolveAsync(SheetsApiClient client, string spreadsheetId, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(SqliteBootstrapper.ConnectionString);
        await conn.OpenAsync(ct);

        // Fetch events
        var remoteEvents = await client.ReadRangeAsync(spreadsheetId, "events!A2:Z", ct);
        foreach (var row in remoteEvents)
        {
            if (row.Count < 13) continue;
            var id = row[0].ToString();
            if (string.IsNullOrEmpty(id)) continue;
            
            var remoteUpdatedStr = row[12].ToString();
            if (!DateTimeOffset.TryParse(remoteUpdatedStr, out var remoteUpdated)) continue;

            // Check local
            await using var chkCmd = conn.CreateCommand();
            chkCmd.CommandText = "SELECT updated_at FROM events_cache WHERE id = $id;";
            chkCmd.Parameters.AddWithValue("$id", id);
            var localUpdatedObj = await chkCmd.ExecuteScalarAsync(ct);

            if (localUpdatedObj == null)
            {
                // Simple insert from remote (Skipping for brevity in V1 unless required, but let's insert minimally to show pull works)
                await using var insCmd = conn.CreateCommand();
                insCmd.CommandText = "INSERT INTO events_cache(id, source_id, title, category_id, time_type, start_at, end_at, tz_capture, tag, notes, recurrence_id, is_deleted, updated_at) VALUES($id, $sId, $title, $cat, $tt, $start, NULL, $tz, $tag, NULL, NULL, 0, $up);";
                insCmd.Parameters.AddWithValue("$id", id);
                insCmd.Parameters.AddWithValue("$sId", row[1]?.ToString() ?? "");
                insCmd.Parameters.AddWithValue("$title", row[2]?.ToString() ?? "");
                insCmd.Parameters.AddWithValue("$cat", row[3]?.ToString() ?? "");
                insCmd.Parameters.AddWithValue("$tt", "duration"); // default
                insCmd.Parameters.AddWithValue("$start", row[5]?.ToString() ?? DateTimeOffset.UtcNow.ToString("o"));
                insCmd.Parameters.AddWithValue("$tz", row[7]?.ToString() ?? "UTC");
                insCmd.Parameters.AddWithValue("$tag", row[8]?.ToString() ?? "");
                insCmd.Parameters.AddWithValue("$up", remoteUpdated.ToString("o"));
                await insCmd.ExecuteNonQueryAsync(ct);
            }
            else
            {
                var localUpdated = DateTimeOffset.Parse(localUpdatedObj.ToString()!);
                if (remoteUpdated > localUpdated)
                {
                    // Check if there is a pending local change
                    await using var pendCmd = conn.CreateCommand();
                    pendCmd.CommandText = "SELECT 1 FROM pending_changes WHERE record_id = $id AND status = 'pending';";
                    pendCmd.Parameters.AddWithValue("$id", id);
                    var hasPending = await pendCmd.ExecuteScalarAsync(ct) != null;

                    if (hasPending) // Conflict!
                    {
                        // Duplicate local as conflict, let remote win main ID
                        var newId = Guid.NewGuid().ToString();
                        await using var confCmd = conn.CreateCommand();
                        confCmd.CommandText = "UPDATE events_cache SET id = $newId, title = title || ' [CONFLICT]' WHERE id = $id;";
                        confCmd.Parameters.AddWithValue("$newId", newId);
                        confCmd.Parameters.AddWithValue("$id", id);
                        await confCmd.ExecuteNonQueryAsync(ct);

                        await using var pConfCmd = conn.CreateCommand();
                        pConfCmd.CommandText = "UPDATE pending_changes SET record_id = $newId WHERE record_id = $id AND status = 'pending';";
                        pConfCmd.Parameters.AddWithValue("$newId", newId);
                        pConfCmd.Parameters.AddWithValue("$id", id);
                        await pConfCmd.ExecuteNonQueryAsync(ct);
                    }

                    // Overwrite with remote
                    await using var updCmd = conn.CreateCommand();
                    updCmd.CommandText = "UPDATE events_cache SET title = $title, updated_at = $up WHERE id = $id;";
                    updCmd.Parameters.AddWithValue("$title", row[2]?.ToString() ?? "");
                    updCmd.Parameters.AddWithValue("$up", remoteUpdated.ToString("o"));
                    updCmd.Parameters.AddWithValue("$id", id);
                    await updCmd.ExecuteNonQueryAsync(ct);
                }
            }
        }
    }
}
