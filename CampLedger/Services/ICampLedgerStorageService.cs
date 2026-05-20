using CampLedger.Models;

namespace CampLedger.Services;

public interface ICampLedgerStorageService
{
    CampLedgerState Load();

    void Save(CampLedgerState state);
}
