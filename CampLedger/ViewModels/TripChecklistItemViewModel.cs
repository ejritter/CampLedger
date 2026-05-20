using System.IO;
using CampLedger.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampLedger.ViewModels;

public sealed partial class TripChecklistItemViewModel : ViewModelBase
{
    public TripChecklistItemViewModel(TripChecklistItem model)
    {
        Model = model;
        IsPhotoExpanded = false;
    }

    public TripChecklistItem Model { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TogglePhotoButtonText))]
    public partial bool IsPhotoExpanded { get; set; }

    public string Name
    {
        get
        {
            return Model.Name;
        }
    }

    public bool IsPacked
    {
        get
        {
            return Model.IsPacked;
        }
        set
        {
            SetProperty(Model.IsPacked, value, Model, static (item, newValue) => item.IsPacked = newValue);
        }
    }

    public bool HasPhoto
    {
        get
        {
            return Model.PhotoData is { Length: > 0 };
        }
    }

    public ImageSource? PhotoSource
    {
        get
        {
            if (!HasPhoto)
            {
                return null;
            }

            return ImageSource.FromStream(() => new MemoryStream(Model.PhotoData!));
        }
    }

    public string TogglePhotoButtonText
    {
        get
        {
            if (IsPhotoExpanded)
            {
                return "Hide Photo";
            }

            return "View Photo";
        }
    }

    [RelayCommand]
    private void TogglePhoto()
    {
        if (!HasPhoto)
        {
            return;
        }

        IsPhotoExpanded = !IsPhotoExpanded;
    }
}
