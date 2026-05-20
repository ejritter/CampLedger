using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CampLedger.ViewModels;

public sealed partial class EditableTripChecklistItemViewModel : ObservableObject
{
    public EditableTripChecklistItemViewModel(Guid itemId, string name, bool isPacked)
    {
        ItemId = itemId;
        Name = name;
        IsPacked = isPacked;
    }

    public Guid ItemId { get; }

    public string Name { get; }

    [ObservableProperty]
    private bool isPacked;
}
