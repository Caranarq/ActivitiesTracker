using ActivitiesTracker.Domain.Enums;

namespace ActivitiesTracker.Domain.Entities;

public sealed class EventRecord : BaseRecord
{
    public string SourceId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public TimeType TimeType { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public string TzCapture { get; set; } = "UTC";
    public string? Tag { get; set; }
    public string? Notes { get; set; }
    public string? RecurrenceId { get; set; }
}
