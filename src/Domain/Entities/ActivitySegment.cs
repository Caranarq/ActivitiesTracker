namespace ActivitiesTracker.Domain.Entities;

public sealed class ActivitySegment : BaseRecord
{
    public string DateKey { get; set; } = string.Empty;
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public string TzCapture { get; set; } = "UTC";
    public string CategoryId { get; set; } = string.Empty;
    public string? LinkedTag { get; set; }
    public string? Notes { get; set; }
}
