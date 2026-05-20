namespace CampLedger.Models;

public sealed class TripChecklistItem
{
    public Guid ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsPacked { get; set; }

    public byte[]? PhotoData { get; set; }
}
