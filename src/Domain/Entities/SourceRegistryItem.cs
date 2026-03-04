using ActivitiesTracker.Domain.Enums;

namespace ActivitiesTracker.Domain.Entities;

public sealed class SourceRegistryItem
{
    public string SourceId { get; init; } = Guid.NewGuid().ToString();
    public SourceType SourceType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string SpreadsheetId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset LastOpenedAt { get; set; } = DateTimeOffset.UtcNow;
}
