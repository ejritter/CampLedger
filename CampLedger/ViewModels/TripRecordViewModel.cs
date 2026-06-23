using CampLedger.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

namespace CampLedger.ViewModels;

public sealed partial class TripRecordViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotEditingNotes))]
    public partial bool IsEditingNotes { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotEditingTripDetails))]
    public partial bool IsEditingTripDetails { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Notes))]
    public partial string EditingNotes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LocationText { get; set; } = "No location set";

    [ObservableProperty]
    public partial string LocationButtonText { get; set; } = "📍 Open Location";

    [ObservableProperty]
    public partial DateTime EditingStartDate { get; set; }

    [ObservableProperty]
    public partial DateTime EditingEndDate { get; set; }

    [ObservableProperty]
    public partial TripLocation? EditingLocation { get; set; }

    [ObservableProperty]
    public partial string EditingLocationText { get; set; } = "No location set";

    [ObservableProperty]
    public partial string EditingLocationButtonText { get; set; } = "📍 Add Location";

    [ObservableProperty]
    public partial bool HasEditingLocation { get; set; }

    public TripRecordViewModel(TripRecord trip)
    {
        Trip = trip;
        EditingNotes = trip.Notes;
        RefreshLocation();
        ResetEditingTripDetails();
    }

    partial void OnIsEditingTripDetailsChanged(bool value)
    {
        Debug.WriteLine($"[DEBUG] TripRecordViewModel.IsEditingTripDetails changed to {value} for trip {TripDurationText}\nStack:\n{Environment.StackTrace}");
    }

    partial void OnIsEditingNotesChanged(bool value)
    {
        Debug.WriteLine($"[DEBUG] TripRecordViewModel.IsEditingNotes changed to {value} for trip {TripDurationText}\nStack:\n{Environment.StackTrace}");
    }

    public TripRecord Trip { get; }

    public DateTime StartDate
    {
        get
        {
            if (Trip.StartDate != default)
            {
                return Trip.StartDate;
            }

            if (Trip.Date != default)
            {
                return Trip.Date;
            }

            return DateTime.Today;
        }
    }

    public DateTime EndDate
    {
        get
        {
            if (Trip.EndDate != default)
            {
                return Trip.EndDate;
            }

            return StartDate.AddDays(1);
        }
    }

    public string TripDurationText
    {
        get
        {
            return $"{StartDate:MMMM dd, yyyy} - {EndDate:MMMM dd, yyyy}";
        }
    }

    public string Notes
    {
        get
        {
            return Trip.Notes;
        }
    }

    public int PackedCount
    {
        get
        {
            return Trip.Items.Count(i => i.IsPacked);
        }
    }

    public int ForgottenCount
    {
        get
        {
            return Trip.Items.Count(i => !i.IsPacked);
        }
    }

    public string PackedItems
    {
        get
        {
            return string.Join(", ", Trip.Items.Where(i => i.IsPacked).Select(i => i.Name));
        }
    }

    public string ForgottenItems
    {
        get
        {
            return string.Join(", ", Trip.Items.Where(i => !i.IsPacked).Select(i => i.Name));
        }
    }

    [ObservableProperty]
    public partial bool HasLocation { get; set; }

    [ObservableProperty]
    public partial bool NoLocation { get; set; }

    public bool IsNotEditingNotes
    {
        get
        {
            return !IsEditingNotes;
        }
    }

    public bool IsNotEditingTripDetails
    {
        get
        {
            return !IsEditingTripDetails;
        }
    }

    partial void OnEditingLocationChanged(TripLocation? value)
    {
        UpdateEditingLocationDisplay();
    }

    public void RefreshNotes()
    {
        // Update the observable EditingNotes which notifies Notes via NotifyPropertyChangedFor
        EditingNotes = Trip.Notes;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StartDate))]
    [NotifyPropertyChangedFor(nameof(EndDate))]
    [NotifyPropertyChangedFor(nameof(TripDurationText))]
    [NotifyPropertyChangedFor(nameof(PackedCount))]
    [NotifyPropertyChangedFor(nameof(ForgottenCount))]
    [NotifyPropertyChangedFor(nameof(PackedItems))]
    [NotifyPropertyChangedFor(nameof(ForgottenItems))]
    public partial int TripStateVersion { get; set; }

    public void RefreshLocation()
    {
        if (!string.IsNullOrWhiteSpace(Trip.Location?.LocationName))
        {
            LocationText = $"Location: {Trip.Location.LocationName}";
            LocationButtonText = $"📍 {Trip.Location.LocationName}";
        }
        else
        {
            LocationText = "No location set";
            // When no location exists, show a simple no-location indicator on the non-edit view
            LocationButtonText = "No location";
        }

        HasLocation = Trip.Location != null;
        NoLocation = !HasLocation;
    }

    public void BeginEditTripDetails()
    {
        ResetEditingTripDetails();
        // also allow editing notes and checklist while editing trip details
        IsEditingTripDetails = true;
        IsEditingNotes = true;
        // create editing buffer for checklist items
        EditingChecklistItems = Trip.Items.Select(i => new EditableTripChecklistItemViewModel(i.ItemId, i.Name, i.IsPacked)).ToList();
    }

    public void CancelEditTripDetails()
    {
        IsEditingTripDetails = false;
        ResetEditingTripDetails();
        // discard editing notes and checklist
        IsEditingNotes = false;
        EditingChecklistItems = null;
    }

    public void CommitEditTripDetails()
    {
        Trip.StartDate = EditingStartDate.Date;
        Trip.EndDate = EditingEndDate.Date;
        Trip.Date = EditingStartDate.Date;
        Trip.Location = CloneLocation(EditingLocation);
        IsEditingTripDetails = false;

        // apply edited notes and checklist back to model
        Trip.Notes = EditingNotes;

        if (EditingChecklistItems != null)
        {
            foreach (var edited in EditingChecklistItems)
            {
                var item = Trip.Items.FirstOrDefault(i => i.ItemId == edited.ItemId);
                if (item != null)
                {
                    item.IsPacked = edited.IsPacked;
                }
            }
        }

        IsEditingNotes = false;
        EditingChecklistItems = null;

        RefreshLocation();
        ResetEditingTripDetails();
        // bump trip state version to notify dependent computed properties
        TripStateVersion++;
    }

    private void ResetEditingTripDetails()
    {
        EditingStartDate = StartDate;
        EditingEndDate = EndDate;
        EditingLocation = CloneLocation(Trip.Location);
        UpdateEditingLocationDisplay();
    }

    [ObservableProperty]
    private List<EditableTripChecklistItemViewModel>? editingChecklistItems;

    private void UpdateEditingLocationDisplay()
    {
        if (!string.IsNullOrWhiteSpace(EditingLocation?.LocationName))
        {
            EditingLocationText = $"Location: {EditingLocation.LocationName}";
            EditingLocationButtonText = $"📍 {EditingLocation.LocationName}";
            HasEditingLocation = true;
            return;
        }

        EditingLocationText = "No location set";
        EditingLocationButtonText = "📍 Add Location";
        HasEditingLocation = false;
    }

    private static TripLocation? CloneLocation(TripLocation? source)
    {
        if (source == null)
        {
            return null;
        }

        return new TripLocation
        {
            LocationName = source.LocationName,
            GoogleMapsUrl = source.GoogleMapsUrl
        };
    }
}
