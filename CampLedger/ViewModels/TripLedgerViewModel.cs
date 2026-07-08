using System.Collections.ObjectModel;
using System.ComponentModel;
using CampLedger.Models;
using CampLedger.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampLedger.ViewModels;

public sealed partial class TripLedgerViewModel : ViewModelBase
{
    private readonly ICampLedgerStateService _stateService;
    private readonly IToastNotificationService _toastService;
    private readonly ITripDurationValidationService _tripDurationValidationService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UnpackedCount))]
    public partial int UnpackedItemsCount { get; set; }

    [ObservableProperty]
    public partial DateTime StartDate { get; set; }

    [ObservableProperty]
    public partial DateTime EndDate { get; set; }

    [ObservableProperty]
    public partial string TripNotes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPhotoPreviewVisible { get; set; } = false;

    [ObservableProperty]
    public partial ImageSource? PhotoPreviewSource { get; set; }

    [ObservableProperty]
    public partial bool IsUnpackedExpanded { get; set; } = true;

    [ObservableProperty]
    public partial bool IsPackedExpanded { get; set; } = true;

    [ObservableProperty]
    public partial string UnpackedSummary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UnpackedToggleText { get; set; } = "Collapse";

    [ObservableProperty]
    public partial string PackedToggleText { get; set; } = "Collapse";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCurrentTripLocation))]
    [NotifyPropertyChangedFor(nameof(NoCurrentTripLocation))]
    public partial TripLocation? CurrentTripLocation { get; set; }

    [ObservableProperty]
    public partial string LocationButtonText { get; set; } = "📍 Add Location";

    [ObservableProperty]
    public partial bool IsLocationPopupOpen { get; set; }

    public bool HasCurrentTripLocation
    {
        get
        {
            return CurrentTripLocation != null;
        }
    }

    public bool NoCurrentTripLocation
    {
        get
        {
            return CurrentTripLocation == null;
        }
    }

    public TripLedgerViewModel(ICampLedgerStateService stateService, IToastNotificationService toastService, ITripDurationValidationService tripDurationValidationService)
    {
        _stateService = stateService;
        _toastService = toastService;
        _tripDurationValidationService = tripDurationValidationService;
        ChecklistItems = new System.Collections.ObjectModel.ObservableCollection<TripChecklistItemViewModel>();
        PackedChecklistItems = new System.Collections.ObjectModel.ObservableCollection<TripChecklistItemViewModel>();
        UnpackedChecklistItems = new System.Collections.ObjectModel.ObservableCollection<TripChecklistItemViewModel>();
        FilteredPackedChecklistItems = new System.Collections.ObjectModel.ObservableCollection<TripChecklistItemViewModel>();
        FilteredUnpackedChecklistItems = new System.Collections.ObjectModel.ObservableCollection<TripChecklistItemViewModel>();

        LoadFromState();
    }

    public ObservableCollection<TripChecklistItemViewModel> ChecklistItems { get; }

    public ObservableCollection<TripChecklistItemViewModel> PackedChecklistItems { get; }

    public ObservableCollection<TripChecklistItemViewModel> UnpackedChecklistItems { get; }

    public ObservableCollection<TripChecklistItemViewModel> FilteredPackedChecklistItems { get; }

    public ObservableCollection<TripChecklistItemViewModel> FilteredUnpackedChecklistItems { get; }

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    public int UnpackedCount
    {
        get
        {
            return UnpackedItemsCount;
        }
    }

    [RelayCommand]
    private void ToggleUnpackedExpanded()
    {
        IsUnpackedExpanded = !IsUnpackedExpanded;
    }

    [RelayCommand]
    private void TogglePackedExpanded()
    {
        IsPackedExpanded = !IsPackedExpanded;
    }

    public void ReloadFromStorage()
    {
        _stateService.Reload();
        LoadFromState();
    }

    [RelayCommand]
    private void RefreshFromHasList()
    {
        LoadFromState();
    }

    partial void OnStartDateChanged(DateTime value)
    {
        if (EndDate.Date < value.Date)
        {
            EndDate = value.Date;
        }
    }

    partial void OnEndDateChanged(DateTime value)
    {
        if (value.Date < StartDate.Date)
        {
            EndDate = StartDate.Date;
        }
    }

    partial void OnUnpackedItemsCountChanged(int value)
    {
        UpdateUnpackedSummary();
    }

    partial void OnIsUnpackedExpandedChanged(bool value)
    {
        UpdateUnpackedToggleText();
    }

    partial void OnIsPackedExpandedChanged(bool value)
    {
        UpdatePackedToggleText();
    }

    partial void OnCurrentTripLocationChanged(TripLocation? value)
    {
        UpdateLocationButtonText();
    }

    private void LoadFromState()
    {
        var state = _stateService.State;
        state.CurrentTrip ??= new TripRecord();

        var currentTrip = state.CurrentTrip;
        var fallbackDate = currentTrip.Date == default ? DateTime.Today : currentTrip.Date;
        StartDate = currentTrip.StartDate == default ? fallbackDate : currentTrip.StartDate;
        EndDate = currentTrip.EndDate == default ? StartDate.AddDays(1) : currentTrip.EndDate;

        if (EndDate.Date < StartDate.Date)
        {
            EndDate = StartDate.Date;
        }

        TripNotes = currentTrip.Notes;
        CurrentTripLocation = currentTrip.Location;
        RefreshDisplayValues();

        SyncChecklistWithHas();
    }

    private void SyncChecklistWithHas()
    {
        var state = _stateService.State;
        var currentTrip = state.CurrentTrip;

        var hasIds = state.Has.Select(i => i.Id).ToHashSet();
        currentTrip.Items.RemoveAll(i => !hasIds.Contains(i.ItemId));

        foreach (var hasItem in state.Has)
        {
            var existing = currentTrip.Items.FirstOrDefault(i => i.ItemId == hasItem.Id);
            if (existing is null)
            {
                currentTrip.Items.Add(new TripChecklistItem
                {
                    ItemId = hasItem.Id,
                    Name = hasItem.Name,
                    IsPacked = false,
                    PhotoData = hasItem.PhotoData
                });
            }
            else
            {
                existing.Name = hasItem.Name;
                existing.PhotoData = hasItem.PhotoData;
            }
        }

        RefreshChecklistLists();
    }

    private void RefreshChecklistLists()
    {
        foreach (var vm in ChecklistItems)
        {
            vm.PropertyChanged -= OnChecklistItemPropertyChanged;
        }

        ChecklistItems.Clear();
        PackedChecklistItems.Clear();
        UnpackedChecklistItems.Clear();
        FilteredPackedChecklistItems.Clear();
        FilteredUnpackedChecklistItems.Clear();

        var state = _stateService.State;
        var currentTrip = state.CurrentTrip;

        foreach (var model in currentTrip.Items.OrderBy(i => i.Name))
        {
            var vm = new TripChecklistItemViewModel(model);
            vm.PropertyChanged += OnChecklistItemPropertyChanged;
            ChecklistItems.Add(vm);

            if (model.IsPacked)
            {
                PackedChecklistItems.Add(vm);
            }
            else
            {
                UnpackedChecklistItems.Add(vm);
            }
        }

        UnpackedItemsCount = ChecklistItems.Count(i => !i.IsPacked);
        ApplySearch(SearchQuery);
        UpdateUnpackedSummary();
    }

    private void RefreshDisplayValues()
    {
        UpdateUnpackedSummary();
        UpdateUnpackedToggleText();
        UpdatePackedToggleText();
        UpdateLocationButtonText();
    }

    private void UpdateUnpackedSummary()
    {
        if (UnpackedCount == 0)
        {
            UnpackedSummary = "Everything is packed!";
            return;
        }

        UnpackedSummary = $"{UnpackedCount} item(s) still needed.";
    }

    private void UpdateUnpackedToggleText()
    {
        if (IsUnpackedExpanded)
        {
            UnpackedToggleText = "Collapse";
            return;
        }

        UnpackedToggleText = "Expand";
    }

    private void UpdatePackedToggleText()
    {
        if (IsPackedExpanded)
        {
            PackedToggleText = "Collapse";
            return;
        }

        PackedToggleText = "Expand";
    }

    private void UpdateLocationButtonText()
    {
        if (!string.IsNullOrWhiteSpace(CurrentTripLocation?.LocationName))
        {
            LocationButtonText = $"📍 {CurrentTripLocation.LocationName}";
            return;
        }

        LocationButtonText = "📍 Add Location";
    }

    [RelayCommand]
    private void Search(string? query)
    {
        ApplySearch(query);
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        ApplySearch(string.Empty);
    }

    private void ApplySearch(string? query)
    {
        FilteredPackedChecklistItems.Clear();
        FilteredUnpackedChecklistItems.Clear();

        var lowerQuery = query?.ToLower() ?? string.Empty;

        foreach (var vm in PackedChecklistItems)
        {
            if (string.IsNullOrWhiteSpace(lowerQuery) || vm.Name.ToLower().Contains(lowerQuery))
            {
                FilteredPackedChecklistItems.Add(vm);
            }
        }

        foreach (var vm in UnpackedChecklistItems)
        {
            if (string.IsNullOrWhiteSpace(lowerQuery) || vm.Name.ToLower().Contains(lowerQuery))
            {
                FilteredUnpackedChecklistItems.Add(vm);
            }
        }
    }

    private void OnChecklistItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TripChecklistItemViewModel.IsPacked))
        {
            return;
        }

        if (sender is not TripChecklistItemViewModel vm)
        {
            return;
        }

        if (vm.IsPacked)
        {
            UnpackedChecklistItems.Remove(vm);
            if (!PackedChecklistItems.Contains(vm))
            {
                PackedChecklistItems.Add(vm);
            }
        }
        else
        {
            PackedChecklistItems.Remove(vm);
            if (!UnpackedChecklistItems.Contains(vm))
            {
                UnpackedChecklistItems.Add(vm);
            }
        }

        UnpackedItemsCount = ChecklistItems.Count(i => !i.IsPacked);
        ApplySearch(SearchQuery);
        SaveTrip();
    }

    internal void SaveTrip()
    {
        var state = _stateService.State;
        state.CurrentTrip.StartDate = StartDate;
        state.CurrentTrip.EndDate = EndDate;
        state.CurrentTrip.Date = StartDate;
        state.CurrentTrip.Notes = TripNotes;
        state.CurrentTrip.Location = CurrentTripLocation;
        state.CurrentTrip.Items = ChecklistItems.Select(i => new TripChecklistItem
        {
            ItemId = i.Model.ItemId,
            Name = i.Model.Name,
            IsPacked = i.IsPacked,
            PhotoData = i.Model.PhotoData
        }).ToList();

        _stateService.Save();
    }

    [RelayCommand]
    private void OpenPhotoPreview(TripChecklistItemViewModel? item)
    {
        if (item is null || !item.HasPhoto)
        {
            return;
        }

        PhotoPreviewSource = item.PhotoSource;
        IsPhotoPreviewVisible = true;
    }

    [RelayCommand]
    private void ClosePhotoPreview()
    {
        IsPhotoPreviewVisible = false;
        PhotoPreviewSource = null;
    }

    private async Task<bool> ValidateTripDurationAsync()
    {
        if (_tripDurationValidationService.IsValid(StartDate, EndDate))
        {
            return true;
        }

        await _toastService.ShowAsync(_tripDurationValidationService.GetValidationMessage());

        return false;
    }

    [RelayCommand]
    private async Task SaveTripWithToast()
    {
        if (!await ValidateTripDurationAsync())
        {
            return;
        }

        SaveTrip();
        await _toastService.ShowAsync("Trip saved successfully!");
    }

    [RelayCommand]
    private async Task CompleteTrip()
    {
        if (!await ValidateTripDurationAsync())
        {
            return;
        }

        SaveTrip();

        var completed = new TripRecord
        {
            Id = Guid.NewGuid(),
            Date = _stateService.State.CurrentTrip.Date,
            StartDate = _stateService.State.CurrentTrip.StartDate,
            EndDate = _stateService.State.CurrentTrip.EndDate,
            Notes = _stateService.State.CurrentTrip.Notes,
            Location = _stateService.State.CurrentTrip.Location,
            Items = _stateService.State.CurrentTrip.Items.Select(i => new TripChecklistItem
            {
                ItemId = i.ItemId,
                Name = i.Name,
                IsPacked = i.IsPacked,
                PhotoData = i.PhotoData
            }).ToList()
        };

        _stateService.State.TripHistory.Add(completed);
        _stateService.State.CurrentTrip = new TripRecord
        {
            Date = DateTime.Today,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            Items = _stateService.State.Has.Select(i => new TripChecklistItem
            {
                ItemId = i.Id,
                Name = i.Name,
                IsPacked = false,
                PhotoData = i.PhotoData
            }).ToList()
        };

        LoadFromState();
        _stateService.Save();
        await _toastService.ShowAsync("Trip completed and saved to history!");
    }

    [RelayCommand]
    private async Task OpenLocationPopup()
    {
        IsLocationPopupOpen = true;
    }

    [RelayCommand]
    private async Task CloseLocationPopup()
    {
        IsLocationPopupOpen = false;
    }

    [RelayCommand]
    private void SetTripLocation(TripLocation? location)
    {
        CurrentTripLocation = location;
        SaveTrip();
    }

    [RelayCommand]
    private async Task ClearTripLocation()
    {
        CurrentTripLocation = null;
        SaveTrip();
        await _toastService.ShowAsync("Trip location cleared.");
    }
}
