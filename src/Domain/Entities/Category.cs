namespace ActivitiesTracker.Domain.Entities;

public sealed class Category : BaseRecord
{
    public string Scope { get; set; } = "both";
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#808080";
    public int SortOrder { get; set; }
}
