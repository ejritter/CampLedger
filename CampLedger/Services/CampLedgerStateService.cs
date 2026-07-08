using System.Diagnostics;
using CampLedger.Models;

namespace CampLedger.Services;

public sealed class CampLedgerStateService : ICampLedgerStateService
{
    private readonly ICampLedgerStorageService _storageService;

    public CampLedgerStateService(ICampLedgerStorageService storageService)
    {
        _storageService = storageService;
        Reload();
    }

    public CampLedgerState State { get; private set; } = new();

    public void Reload()
    {
        try
        {
            State = _storageService.Load();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"State reload failed: {ex.Message}");
            State = new CampLedgerState();
        }
    }

    public void Save()
    {
        _storageService.Save(State);
    }
}
