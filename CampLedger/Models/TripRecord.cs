namespace CampLedger.Models;

public sealed class TripRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime Date { get; set; } = DateTime.Today;

    public DateTime StartDate { get; set; } = DateTime.Today;

    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(1);

    public string Notes { get; set; } = string.Empty;

    public TripLocation? Location { get; set; }

    public List<TripChecklistItem> Items { get; set; } = [];
}
