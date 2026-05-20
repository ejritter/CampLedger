using CampLedger.Models;

namespace CampLedger.Services;

public sealed class CampLedgerStateService : ICampLedgerStateService
{
    private readonly ICampLedgerStorageService _storageService;

    public CampLedgerStateService(ICampLedgerStorageService storageService)
    {
        _storageService = storageService;
        State = _storageService.Load();
    }

    public CampLedgerState State { get; }

    public void Save()
    {
        _storageService.Save(State);
    }
}
