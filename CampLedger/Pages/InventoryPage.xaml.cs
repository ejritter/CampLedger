using System.IO;
using CampLedger.Models;
using CampLedger.Services;
using CampLedger.ViewModels;
using CommunityToolkit.Maui.Extensions;

namespace CampLedger.Pages;

public partial class InventoryPage : ContentPage
{
    private const string DragItemIdKey = "DragItemId";
    private const string DragBucketKey = "DragBucket";
    private CancellationTokenSource? _searchDebounceCts;


    public InventoryPage(InventoryViewModel viewModel)
    {
        InitializeComponent();
        this.AttachViewModel(viewModel);
        ViewModel.EditRequested += OnEditRequested;
        ViewModel.PhotoPreviewRequested += OnPhotoPreviewRequested;
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
            var shouldReplace = await this.GetPresentingPage().DisplayAlertAsync("Replace Photo", "This item already has a photo. Do you want to replace it?", "Replace", "Cancel");
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
            await this.GetPresentingPage().DisplayAlertAsync("Permission Needed", "Camera permission is required to take photos.", "OK");
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
                var useGallery = await this.GetPresentingPage().DisplayAlertAsync("Camera Unavailable", "Camera capture is unavailable on this device. Pick a photo from your gallery instead?", "Pick Photo", "Cancel");
                if (!useGallery)
                {
                    return;
                }

                var pickedPhotos = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
                {
                    SelectionLimit = 1
                });
                photo = pickedPhotos?.FirstOrDefault();
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
                var useGallery = await this.GetPresentingPage().DisplayAlertAsync("Camera Unavailable", "Photo capture is unavailable. Pick a photo from your gallery instead?", "Pick Photo", "Cancel");
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
                await this.GetPresentingPage().DisplayAlertAsync("Photo Error", "Unable to capture or pick a photo right now.", "OK");
            }
        }
        catch (PermissionException)
        {
            await this.GetPresentingPage().DisplayAlertAsync("Permission Needed", "Camera permission is required to take photos.", "OK");
        }
        catch (Exception)
        {
            await this.GetPresentingPage().DisplayAlertAsync("Photo Error", "Unable to capture photo right now.", "OK");
        }
    }

    private async void OnRemoveItemClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not InventoryItemViewModel item)
        {
            return;
        }

        bool confirmed = await this.GetPresentingPage().DisplayAlertAsync("Remove Item", $"Are you sure you want to remove \"{item.Name}\"?", "Remove", "Cancel");
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
        ViewModel.BeginDrag(item);
    }

    private void OnItemDropCompleted(object? sender, DropCompletedEventArgs e)
    {
        ViewModel.EndDrag();
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
        ResetDropZoneBackground(sender);
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

        bool confirmed = await this.GetPresentingPage().DisplayAlertAsync("Remove Photo", $"Are you sure you want to remove this photo?", "Remove", "Cancel");
        if (confirmed)
        {
            ViewModel.RemovePhotoFromItemCommand.Execute(item);
        }
    }

    private void HandleZoneDrop(DropEventArgs e, InventoryBucket toBucket, object? sender)
    {
        ResetDropZoneBackground(sender);

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

    private static void ResetDropZoneBackground(object? sender)
    {
        if (sender is not Border zone)
        {
            return;
        }

        zone.ClearValue(VisualElement.BackgroundColorProperty);
        zone.SetDynamicResource(VisualElement.BackgroundColorProperty, "SurfaceColor");
    }

    private async void OnEditRequested(object? sender, EventArgs e)
    {
        var popup = new InventoryItemEditPopup(ViewModel);
        await this.GetPresentingPage().ShowPopupAsync(popup);
    }

    private async void OnPhotoPreviewRequested(object? sender, EventArgs e)
    {
        var popup = new InventoryPhotoPreviewPopup(ViewModel);
        await this.GetPresentingPage().ShowPopupAsync(popup);
    }

    private void OnEditItemClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not InventoryItemViewModel item)
        {
            return;
        }

        ViewModel.EditItemCommand.Execute(item);
    }

    private void OnPhotoPreviewClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not InventoryItemViewModel item)
        {
            return;
        }

        ViewModel.OpenPhotoPreviewCommand.Execute(item);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ViewModel.EditRequested -= OnEditRequested;
        ViewModel.PhotoPreviewRequested -= OnPhotoPreviewRequested;
    }
}
