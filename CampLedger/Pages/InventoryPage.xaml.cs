using System.IO;
using CampLedger.Models;
using CampLedger.Services;
using CampLedger.ViewModels;

namespace CampLedger.Pages;

public partial class InventoryPage : ContentPage
{
    private const string DragItemIdKey = "DragItemId";
    private const string DragBucketKey = "DragBucket";
    private CancellationTokenSource? _searchDebounceCts;

    public InventoryPage()
        : this(CreateViewModel())
    {
    }

    public InventoryPage(InventoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private static InventoryViewModel CreateViewModel()
    {
        try
        {
            return ServiceHelper.GetService<InventoryViewModel>();
        }
        catch
        {
            var storageService = new CampLedgerStorageService();
            var stateService = new CampLedgerStateService(storageService);
            return new InventoryViewModel(stateService);
        }
    }

    private InventoryViewModel ViewModel
    {
        get
        {
            return (InventoryViewModel)BindingContext;
        }
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            _searchDebounceCts?.Cancel();
            ViewModel.SearchCommand.Execute(string.Empty);
            return;
        }

        _searchDebounceCts?.Cancel();
        _searchDebounceCts = new CancellationTokenSource();
        var cancellationToken = _searchDebounceCts.Token;

        _ = DebounceSearchAsync(e.NewTextValue, cancellationToken);
    }

    private async Task DebounceSearchAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        ViewModel.SearchCommand.Execute(query);
    }

    private void OnClearSearchClicked(object? sender, EventArgs e)
    {
        SearchBar.Text = string.Empty;
        ViewModel.SearchCommand.Execute(string.Empty);
    }

    private async void OnAddPhotoClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not InventoryItemViewModel item)
        {
            return;
        }

        if (item.HasPhoto)
        {
            var shouldReplace = await DisplayAlertAsync("Replace Photo", "This item already has a photo. Do you want to replace it?", "Replace", "Cancel");
            if (!shouldReplace)
            {
                return;
            }
        }

        var permissionStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (permissionStatus != PermissionStatus.Granted)
        {
            permissionStatus = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (permissionStatus != PermissionStatus.Granted)
        {
            await DisplayAlertAsync("Permission Needed", "Camera permission is required to take photos.", "OK");
            return;
        }

        try
        {
            FileResult? photo;

            if (MediaPicker.Default.IsCaptureSupported)
            {
                photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo is null)
                {
                    return;
                }
            }
            else
            {
                var useGallery = await DisplayAlertAsync("Camera Unavailable", "Camera capture is unavailable on this device. Pick a photo from your gallery instead?", "Pick Photo", "Cancel");
                if (!useGallery)
                {
                    return;
                }

                photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo is null)
                {
                    return;
                }
            }

            await using var photoStream = await photo.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await photoStream.CopyToAsync(memoryStream);

            ViewModel.SetItemPhoto(item.Id, item.Bucket, memoryStream.ToArray());
        }
        catch (FeatureNotSupportedException)
        {
            try
            {
                var useGallery = await DisplayAlertAsync("Camera Unavailable", "Photo capture is unavailable. Pick a photo from your gallery instead?", "Pick Photo", "Cancel");
                if (!useGallery)
                {
                    return;
                }

                var pickedPhotos = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
                {
                    SelectionLimit = 1
                });
                if (pickedPhotos is null)
                {
                    return;
                }

                await using var photoStream = await pickedPhotos.First().OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await photoStream.CopyToAsync(memoryStream);

                ViewModel.SetItemPhoto(item.Id, item.Bucket, memoryStream.ToArray());
            }
            catch (Exception)
            {
                await DisplayAlertAsync("Photo Error", "Unable to capture or pick a photo right now.", "OK");
            }
        }
        catch (PermissionException)
        {
            await DisplayAlertAsync("Permission Needed", "Camera permission is required to take photos.", "OK");
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Photo Error", "Unable to capture photo right now.", "OK");
        }
    }

    private async void OnRemoveItemClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not InventoryItemViewModel item)
        {
            return;
        }

        bool confirmed = await DisplayAlertAsync("Remove Item", $"Are you sure you want to remove \"{item.Name}\"?", "Remove", "Cancel");
        if (confirmed)
        {
            ViewModel.RemoveItemCommand.Execute(item);
        }
    }

    private void OnItemDragStarting(object? sender, DragStartingEventArgs e)
    {
        if (sender is not Border border || border.BindingContext is not InventoryItemViewModel item)
        {
            return;
        }

        e.Data.Properties[DragItemIdKey] = item.Id.ToString();
        e.Data.Properties[DragBucketKey] = item.Bucket.ToString();
    }

    private void OnNeedsDropZoneDragOver(object? sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        if (sender is Border zone)
        {
            zone.BackgroundColor = Color.FromArgb("#D8CCFF");
        }
    }

    private void OnWantsDropZoneDragOver(object? sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        if (sender is Border zone)
        {
            zone.BackgroundColor = Color.FromArgb("#D8CCFF");
        }
    }

    private void OnHasDropZoneDragOver(object? sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        if (sender is Border zone)
        {
            zone.BackgroundColor = Color.FromArgb("#D8CCFF");
        }
    }

    private void OnDropZoneDragLeave(object? sender, DragEventArgs e)
    {
        if (sender is Border zone)
        {
            zone.BackgroundColor = Colors.Transparent;
        }
    }

    private void OnNeedsDropZoneDrop(object? sender, DropEventArgs e)
    {
        HandleZoneDrop(e, InventoryBucket.Needs, sender);
    }

    private void OnWantsDropZoneDrop(object? sender, DropEventArgs e)
    {
        HandleZoneDrop(e, InventoryBucket.Wants, sender);
    }

    private void OnHasDropZoneDrop(object? sender, DropEventArgs e)
    {
        HandleZoneDrop(e, InventoryBucket.Has, sender);
    }


    private async void RemovePhotoFromItem(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not InventoryItemViewModel item)
        {
            return;
        }

        bool confirmed = await DisplayAlertAsync("Remove Photo", $"Are you sure you want to remove this photo?", "Remove", "Cancel");
        if (confirmed)
        {
            ViewModel.RemovePhotoFromItemCommand.Execute(item);
        }
    }

    private void HandleZoneDrop(DropEventArgs e, InventoryBucket toBucket, object? sender)
    {
        if (sender is Border zone)
        {
            zone.BackgroundColor = Colors.Transparent;
        }

        if (!e.Data.Properties.TryGetValue(DragItemIdKey, out var itemIdRaw) ||
            !e.Data.Properties.TryGetValue(DragBucketKey, out var bucketRaw) ||
            itemIdRaw is not string itemIdText ||
            bucketRaw is not string bucketText ||
            !Guid.TryParse(itemIdText, out var itemId) ||
            !Enum.TryParse<InventoryBucket>(bucketText, out var fromBucket))
        {
            return;
        }

        ViewModel.MoveItem(itemId, fromBucket, toBucket);
    }
}
