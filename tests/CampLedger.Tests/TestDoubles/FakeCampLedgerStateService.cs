using CampLedger.Models;
using CampLedger.Services;

namespace CampLedger.Tests.TestDoubles;

public sealed class FakeCampLedgerStateService : CampLedger.Services.ICampLedgerStateService
{
    public CampLedgerState State { get; set; }

    public FakeCampLedgerStateService()
    {
        State = new CampLedgerState
        {
            Needs = [],
            Wants = [],
            Has = [],
            CurrentTrip = null!,
            TripHistory = []
        };
    }

    public FakeCampLedgerStateService(CampLedgerState initialState)
    {
        State = initialState;
    }

    public void Reload()
    {
        // No-op for fake service
    }

    public void Save()
    {
        // No-op for fake service
    }
}
