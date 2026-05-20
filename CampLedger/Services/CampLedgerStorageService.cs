using System.Text.Json;
using CampLedger.Models;

namespace CampLedger.Services;

public sealed class CampLedgerStorageService : ICampLedgerStorageService
{
    private const string StateKey = "camp-ledger-state";

    public CampLedgerState Load()
    {
        var json = Preferences.Default.Get(StateKey, string.Empty);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new CampLedgerState();
        }

        var state = JsonSerializer.Deserialize<CampLedgerState>(json);

        return state ?? new CampLedgerState();
    }

    public void Save(CampLedgerState state)
    {
        var json = JsonSerializer.Serialize(state);
        Preferences.Default.Set(StateKey, json);
    }
}
