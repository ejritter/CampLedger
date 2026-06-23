using System.IO;
using CampLedger.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampLedger.ViewModels;

public sealed partial class InventoryItemViewModel : ViewModelBase
{
    private ImageSource? _photoSource;

    public InventoryItemViewModel(InventoryItem item, InventoryBucket bucket)
    {
        Item = item;
        Bucket = bucket;
        PhotoData = item.PhotoData;
        IsPhotoExpanded = false;
        UpdatePhotoSource();
    }

    public InventoryItem Item { get; }

    [ObservableProperty]
    public partial InventoryBucket Bucket { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TogglePhotoButtonText))]
    public partial bool IsPhotoExpanded { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    [NotifyPropertyChangedFor(nameof(PhotoSource))]
    [NotifyPropertyChangedFor(nameof(TogglePhotoButtonText))]
    [NotifyPropertyChangedFor(nameof(PhotoActionText))]
    public partial byte[]? PhotoData { get; set; }

    public Guid Id
    {
        get
        {
            return Item.Id;
        }
    }

    public string Name
    {
        get
        {
            return Item.Name;
        }
        set
        {
            SetProperty(Item.Name, value, Item, static (model, newValue) => model.Name = newValue);
        }
    }

    public bool HasPhoto
    {
        get
        {
            return PhotoData is { Length: > 0 };
        }
    }

    public ImageSource? PhotoSource
    {
        get
        {
            return _photoSource;
        }
    }

    private void UpdatePhotoSource()
    {
        if (!HasPhoto)
        {
            _photoSource = null;
        }
        else
        {
            _photoSource = ImageSource.FromStream(() => new MemoryStream(PhotoData!));
        }
    }

    public string TogglePhotoButtonText
    {
        get
        {
            if (!HasPhoto)
            {
                return "Expand";
            }

            return IsPhotoExpanded ? "Collapse" : "Expand";
        }
    }

    public string PhotoActionText
    {
        get
        {
            return HasPhoto ? "📸 Replace Photo" : "📷 Add Photo";
        }
    }

    partial void OnPhotoDataChanged(byte[]? value)
    {
        Item.PhotoData = value;
        IsPhotoExpanded = false;
        UpdatePhotoSource();
    }

    partial void OnBucketChanged(InventoryBucket value)
    {
        Item.Bucket = value;
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
