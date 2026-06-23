using System.Text.Json;
using CampLedger.Models;

namespace CampLedger.Services;

public sealed class CampLedgerStorageService : ICampLedgerStorageService
{
    private const string StateKey = "camp-ledger-state";
    private readonly object _lock = new();

    public CampLedgerState Load()
    {
        string json;
        lock (_lock)
        {
            json = Preferences.Default.Get(StateKey, string.Empty);
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            return new CampLedgerState();
        }

        try
        {
            var state = JsonSerializer.Deserialize<CampLedgerState>(json);
            return state ?? new CampLedgerState();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deserializing CampLedgerState: {ex.Message}");
            return new CampLedgerState();
        }
    }

    public void Save(CampLedgerState state)
    {
        // Serialize to JSON on the caller thread. If the state includes photo byte arrays, 
        // this is CPU-intensive. So doing serialization and writing synchronously is done, 
        // but now we'll allow calling save.
        var json = JsonSerializer.Serialize(state);
        lock (_lock)
        {
            Preferences.Default.Set(StateKey, json);
        }
    }
}
