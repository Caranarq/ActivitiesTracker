using ActivitiesTracker.Infrastructure.Config;
using Microsoft.Data.Sqlite;

namespace ActivitiesTracker.Infrastructure.Data;

public static class SqliteBootstrapper
{
    public static string ConnectionString => $"Data Source={AppPaths.DatabasePath}";

    public static void EnsureCreated()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS source_registry (
    source_id TEXT PRIMARY KEY,
    source_type TEXT NOT NULL,
    display_name TEXT NOT NULL,
    spreadsheet_id TEXT NOT NULL,
    is_active INTEGER NOT NULL,
    last_opened_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS events_cache (
    id TEXT PRIMARY KEY,
    source_id TEXT NOT NULL,
    title TEXT NOT NULL,
    category_id TEXT NOT NULL,
    time_type TEXT NOT NULL,
    start_at TEXT NOT NULL,
    end_at TEXT NULL,
    tz_capture TEXT NOT NULL,
    tag TEXT NULL,
    notes TEXT NULL,
    recurrence_id TEXT NULL,
    is_deleted INTEGER NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS activity_segments_cache (
    id TEXT PRIMARY KEY,
    date_key TEXT NOT NULL,
    start_at TEXT NOT NULL,
    end_at TEXT NOT NULL,
    tz_capture TEXT NOT NULL,
    category_id TEXT NOT NULL,
    linked_tag TEXT NULL,
    notes TEXT NULL,
    is_deleted INTEGER NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS pending_changes (
    queue_id TEXT PRIMARY KEY,
    source_id TEXT NOT NULL,
    tab_name TEXT NOT NULL,
    operation TEXT NOT NULL,
    record_id TEXT NOT NULL,
    payload_json TEXT NOT NULL,
    created_at TEXT NOT NULL,
    status TEXT NOT NULL,
    error_message TEXT NULL
);";
        command.ExecuteNonQuery();
    }
}
