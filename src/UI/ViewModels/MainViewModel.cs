using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using ActivitiesTracker.Domain.Entities;
using ActivitiesTracker.Domain.Enums;
using ActivitiesTracker.Infrastructure.Config;
using ActivitiesTracker.Infrastructure.Data;
using ActivitiesTracker.Infrastructure.Logging;
using ActivitiesTracker.Sync.Services;
using Microsoft.Data.Sqlite;

namespace ActivitiesTracker.UI.ViewModels;

public sealed class MainViewModel
{
    private readonly FileLogSink _log = new();
    private readonly SyncService _sync;

    public MainViewModel()
    {
        _sync = new SyncService(_log);
        AddOpenEndedEventCommand = new RelayCommand(AddOpenEndedEvent);
        AddActivitySegmentCommand = new RelayCommand(AddActivitySegment);
        SyncNowCommand = new RelayCommand(SyncNow);

        LoadCache();
    }

    public string Connectivity => "Offline-first";
    public int PendingCount { get; private set; }
    public string DatabasePath => AppPaths.DatabasePath;
    public string CredentialsPath => AppPaths.CredentialsPath;

    public ObservableCollection<EventRecord> Events { get; } = [];
    public ObservableCollection<ActivitySegment> Segments { get; } = [];

    public ICommand AddOpenEndedEventCommand { get; }
    public ICommand AddActivitySegmentCommand { get; }
    public ICommand SyncNowCommand { get; }

    private async void AddOpenEndedEvent()
    {
        var now = DateTimeOffset.Now;
        var ev = new EventRecord
        {
            SourceId = "local-events",
            Title = "Open-ended activity",
            CategoryId = "default",
            TimeType = TimeType.OpenEnded,
            StartAt = now,
            EndAt = null,
            TzCapture = TimeZoneInfo.Local.Id,
            Tag = "MANUAL",
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await using var conn = new SqliteConnection(SqliteBootstrapper.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO events_cache(id, source_id, title, category_id, time_type, start_at, end_at, tz_capture, tag, notes, recurrence_id, is_deleted, updated_at)
VALUES ($id, $sourceId, $title, $categoryId, $timeType, $startAt, NULL, $tzCapture, $tag, NULL, NULL, 0, $updatedAt);";
        cmd.Parameters.AddWithValue("$id", ev.Id);
        cmd.Parameters.AddWithValue("$sourceId", ev.SourceId);
        cmd.Parameters.AddWithValue("$title", ev.Title);
        cmd.Parameters.AddWithValue("$categoryId", ev.CategoryId);
        cmd.Parameters.AddWithValue("$timeType", ev.TimeType.ToString().ToLowerInvariant());
        cmd.Parameters.AddWithValue("$startAt", ev.StartAt.ToString("o"));
        cmd.Parameters.AddWithValue("$tzCapture", ev.TzCapture);
        cmd.Parameters.AddWithValue("$tag", ev.Tag ?? string.Empty);
        cmd.Parameters.AddWithValue("$updatedAt", ev.UpdatedAt.ToString("o"));
        await cmd.ExecuteNonQueryAsync();

        // Explicit separation: no auto creation of activity_segments.
        await _sync.QueuePendingChangeAsync(ev.SourceId, "events", "insert", ev.Id, JsonSerializer.Serialize(ev));

        Events.Insert(0, ev);
        PendingCount++;
    }

    private async void AddActivitySegment()
    {
        var now = DateTimeOffset.Now;
        var end = now.AddHours(1);
        var segment = new ActivitySegment
        {
            DateKey = now.ToString("yyyy-MM-dd"),
            StartAt = now,
            EndAt = end,
            TzCapture = TimeZoneInfo.Local.Id,
            CategoryId = "sleep",
            LinkedTag = "",
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await using var conn = new SqliteConnection(SqliteBootstrapper.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO activity_segments_cache(id, date_key, start_at, end_at, tz_capture, category_id, linked_tag, notes, is_deleted, updated_at)
VALUES ($id, $dateKey, $startAt, $endAt, $tzCapture, $categoryId, $linkedTag, NULL, 0, $updatedAt);";
        cmd.Parameters.AddWithValue("$id", segment.Id);
        cmd.Parameters.AddWithValue("$dateKey", segment.DateKey);
        cmd.Parameters.AddWithValue("$startAt", segment.StartAt.ToString("o"));
        cmd.Parameters.AddWithValue("$endAt", segment.EndAt.ToString("o"));
        cmd.Parameters.AddWithValue("$tzCapture", segment.TzCapture);
        cmd.Parameters.AddWithValue("$categoryId", segment.CategoryId);
        cmd.Parameters.AddWithValue("$linkedTag", segment.LinkedTag ?? string.Empty);
        cmd.Parameters.AddWithValue("$updatedAt", segment.UpdatedAt.ToString("o"));
        await cmd.ExecuteNonQueryAsync();

        await _sync.QueuePendingChangeAsync("activity-source", "activity_segments", "insert", segment.Id, JsonSerializer.Serialize(segment));

        Segments.Insert(0, segment);
        PendingCount++;
    }

    private async void SyncNow()
    {
        try
        {
            var synced = await _sync.SyncNowAsync();
            PendingCount = Math.Max(0, PendingCount - synced);
            MessageBox.Show($"Sync complete. Processed {synced} pending change(s).", "Sync", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _log.Error("SyncNow failed", ex);
            if (ex.Message.Contains("Authentication required"))
            {
                MessageBox.Show($"Google Authentication required. Please place your credentials.json file in:\n{AppPaths.CredentialsPath}", "Sync", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show($"Sync error: {ex.Message}", "Sync", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void LoadCache()
    {
        try
        {
            using var conn = new SqliteConnection(SqliteBootstrapper.ConnectionString);
            conn.Open();

            using var eventsCmd = conn.CreateCommand();
            eventsCmd.CommandText = "SELECT id, source_id, title, category_id, time_type, start_at, end_at, tz_capture, tag, notes, recurrence_id, is_deleted, updated_at FROM events_cache WHERE is_deleted = 0 ORDER BY updated_at DESC LIMIT 100;";
            using var reader = eventsCmd.ExecuteReader();
            while (reader.Read())
            {
                Events.Add(new EventRecord
                {
                    Id = reader.GetString(0),
                    SourceId = reader.GetString(1),
                    Title = reader.GetString(2),
                    CategoryId = reader.GetString(3),
                    TimeType = Enum.TryParse<TimeType>(reader.GetString(4), true, out var t) ? t : TimeType.Duration,
                    StartAt = DateTimeOffset.Parse(reader.GetString(5)),
                    EndAt = reader.IsDBNull(6) ? null : DateTimeOffset.Parse(reader.GetString(6)),
                    TzCapture = reader.GetString(7),
                    Tag = reader.IsDBNull(8) ? null : reader.GetString(8),
                    Notes = reader.IsDBNull(9) ? null : reader.GetString(9),
                    RecurrenceId = reader.IsDBNull(10) ? null : reader.GetString(10),
                    IsDeleted = reader.GetInt32(11) == 1,
                    UpdatedAt = DateTimeOffset.Parse(reader.GetString(12))
                });
            }

            using var segmentsCmd = conn.CreateCommand();
            segmentsCmd.CommandText = "SELECT id, date_key, start_at, end_at, tz_capture, category_id, linked_tag, notes, is_deleted, updated_at FROM activity_segments_cache WHERE is_deleted = 0 ORDER BY updated_at DESC LIMIT 100;";
            using var segmentReader = segmentsCmd.ExecuteReader();
            while (segmentReader.Read())
            {
                Segments.Add(new ActivitySegment
                {
                    Id = segmentReader.GetString(0),
                    DateKey = segmentReader.GetString(1),
                    StartAt = DateTimeOffset.Parse(segmentReader.GetString(2)),
                    EndAt = DateTimeOffset.Parse(segmentReader.GetString(3)),
                    TzCapture = segmentReader.GetString(4),
                    CategoryId = segmentReader.GetString(5),
                    LinkedTag = segmentReader.IsDBNull(6) ? null : segmentReader.GetString(6),
                    Notes = segmentReader.IsDBNull(7) ? null : segmentReader.GetString(7),
                    IsDeleted = segmentReader.GetInt32(8) == 1,
                    UpdatedAt = DateTimeOffset.Parse(segmentReader.GetString(9))
                });
            }

            using var pendingCmd = conn.CreateCommand();
            pendingCmd.CommandText = "SELECT COUNT(1) FROM pending_changes WHERE status = 'pending';";
            PendingCount = Convert.ToInt32(pendingCmd.ExecuteScalar());
        }
        catch (Exception ex)
        {
            _log.Error("LoadCache failed", ex);
            MessageBox.Show($"Could not load cached data: {ex.Message}", "ActivitiesTracker", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
