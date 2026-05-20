namespace CampLedger.Models;

public sealed class CampLedgerState
{
    public List<InventoryItem> Needs { get; set; } = [];

    public List<InventoryItem> Wants { get; set; } = [];

    public List<InventoryItem> Has { get; set; } = [];

    public TripRecord CurrentTrip { get; set; } = new();

    public List<TripRecord> TripHistory { get; set; } = [];
}
