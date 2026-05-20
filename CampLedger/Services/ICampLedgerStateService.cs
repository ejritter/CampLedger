using CampLedger.Models;

namespace CampLedger.Services;

public interface ICampLedgerStateService
{
    CampLedgerState State { get; }

    void Save();
}
