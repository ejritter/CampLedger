namespace CampLedger.Models;

public sealed class InventoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public InventoryBucket Bucket { get; set; } = InventoryBucket.Needs;

    public byte[]? PhotoData { get; set; }
}
