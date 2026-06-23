using System.Diagnostics;
using CampLedger.Models;

namespace CampLedger.Services;

public sealed class CampLedgerStateService : ICampLedgerStateService
{
    private readonly ICampLedgerStorageService _storageService;

    public CampLedgerStateService(ICampLedgerStorageService storageService)
    {
        _storageService = storageService;
        try
        {
            State = _storageService.Load();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"State initialization failed: {ex.Message}");
            State = new CampLedgerState();
        }
    }

    public CampLedgerState State { get; }

    public void Save()
    {
        _storageService.Save(State);
    }
}
