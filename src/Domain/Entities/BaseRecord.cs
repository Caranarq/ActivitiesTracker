namespace ActivitiesTracker.Domain.Entities;

public abstract class BaseRecord
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? MetaJson { get; set; }
}
