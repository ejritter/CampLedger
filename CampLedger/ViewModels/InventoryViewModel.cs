using System.Collections.ObjectModel;
using CampLedger.Models;
using CampLedger.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampLedger.ViewModels;

public sealed partial class InventoryViewModel : ViewModelBase
{
    private readonly ICampLedgerStateService _stateService;

    [ObservableProperty]
    public partial string NewNeedsItemName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewWantsItemName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewHasItemName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    public partial InventoryItemViewModel? EditingItem { get; set; }

    [ObservableProperty]
    public partial string EditingItemName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPhotoPreviewVisible { get; set; }

    [ObservableProperty]
    public partial InventoryItemViewModel? PhotoPreviewItem { get; set; }

    [ObservableProperty]
    public partial ImageSource? PhotoPreviewSource { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NeedsToggleText))]
    public partial bool IsNeedsExpanded { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WantsToggleText))]
    public partial bool IsWantsExpanded { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasToggleText))]
    public partial bool IsHasExpanded { get; set; } = true;

    public InventoryViewModel(ICampLedgerStateService stateService)
    {
        _stateService = stateService;

        NeedsItems = new ObservableCollection<InventoryItemViewModel>(_stateService.State.Needs.Select(i => new InventoryItemViewModel(i, InventoryBucket.Needs)));
        WantsItems = new ObservableCollection<InventoryItemViewModel>(_stateService.State.Wants.Select(i => new InventoryItemViewModel(i, InventoryBucket.Wants)));
        HasItems = new ObservableCollection<InventoryItemViewModel>(_stateService.State.Has.Select(i => new InventoryItemViewModel(i, InventoryBucket.Has)));
    }

    public ObservableCollection<InventoryItemViewModel> NeedsItems { get; }

    public ObservableCollection<InventoryItemViewModel> WantsItems { get; }

    public ObservableCollection<InventoryItemViewModel> HasItems { get; }

    public bool IsEditing
    {
        get
        {
            return EditingItem is not null;
        }
    }

    public string NeedsToggleText
    {
        get
        {
            if (IsNeedsExpanded)
            {
                return "Collapse";
            }

            return "Expand";
        }
    }

    public string WantsToggleText
    {
        get
        {
            if (IsWantsExpanded)
            {
                return "Collapse";
            }

            return "Expand";
        }
    }

    public string HasToggleText
    {
        get
        {
            if (IsHasExpanded)
            {
                return "Collapse";
            }

            return "Expand";
        }
    }

    [RelayCommand]
    private void ToggleNeedsExpanded()
    {
        IsNeedsExpanded = !IsNeedsExpanded;
    }

    [RelayCommand]
    private void ToggleWantsExpanded()
    {
        IsWantsExpanded = !IsWantsExpanded;
    }

    [RelayCommand]
    private void ToggleHasExpanded()
    {
        IsHasExpanded = !IsHasExpanded;
    }

    public void MoveItem(Guid itemId, InventoryBucket fromBucket, InventoryBucket toBucket)
    {
        if (fromBucket == toBucket)
        {
            return;
        }

        var fromList = GetStateList(fromBucket);
        var model = fromList.FirstOrDefault(i => i.Id == itemId);
        if (model is null)
        {
            return;
        }

        fromList.Remove(model);
        GetStateList(toBucket).Add(model);

        var fromCollection = GetCollection(fromBucket);
        var fromVm = fromCollection.FirstOrDefault(i => i.Id == itemId);
        if (fromVm is not null)
        {
            fromCollection.Remove(fromVm);
        }

        GetCollection(toBucket).Add(new InventoryItemViewModel(model, toBucket));
        Persist();
    }

    [RelayCommand]
    private void AddNeedsItem()
    {
        AddItem(NewNeedsItemName, InventoryBucket.Needs);
        NewNeedsItemName = string.Empty;
    }

    [RelayCommand]
    private void AddWantsItem()
    {
        AddItem(NewWantsItemName, InventoryBucket.Wants);
        NewWantsItemName = string.Empty;
    }

    [RelayCommand]
    private void AddHasItem()
    {
        AddItem(NewHasItemName, InventoryBucket.Has);
        NewHasItemName = string.Empty;
    }

    [RelayCommand]
    private void RemoveItem(InventoryItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        var stateList = GetStateList(item.Bucket);
        var model = stateList.FirstOrDefault(i => i.Id == item.Id);
        if (model is not null)
        {
            stateList.Remove(model);
        }

        GetCollection(item.Bucket).Remove(item);
        Persist();
    }

    [RelayCommand]
    private async Task RemovePhotoFromItem(InventoryItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }
        
        item.PhotoData = null;
    }

    [RelayCommand]
    private void Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            RefreshAllItems();
            return;
        }

        var lowerQuery = query.ToLower();
        var allItems = new List<InventoryItemViewModel>();

        foreach (var item in _stateService.State.Needs)
        {
            if (item.Name.ToLower().Contains(lowerQuery))
            {
                allItems.Add(new InventoryItemViewModel(item, InventoryBucket.Needs));
            }
        }

        foreach (var item in _stateService.State.Wants)
        {
            if (item.Name.ToLower().Contains(lowerQuery))
            {
                allItems.Add(new InventoryItemViewModel(item, InventoryBucket.Wants));
            }
        }

        foreach (var item in _stateService.State.Has)
        {
            if (item.Name.ToLower().Contains(lowerQuery))
            {
                allItems.Add(new InventoryItemViewModel(item, InventoryBucket.Has));
            }
        }

        NeedsItems.Clear();
        WantsItems.Clear();
        HasItems.Clear();

        foreach (var item in allItems)
        {
            GetCollection(item.Bucket).Add(item);
        }
    }

    [RelayCommand]
    private void EditItem(InventoryItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        EditingItem = item;
        EditingItemName = item.Name;
    }

    [RelayCommand]
    private void SaveEdit()
    {
        if (EditingItem is null)
        {
            return;
        }

        var trimmed = EditingItemName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            CancelEdit();
            return;
        }

        EditingItem.Name = trimmed;
        Persist();
        CancelEdit();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        EditingItem = null;
        EditingItemName = string.Empty;
    }

    private void RefreshAllItems()
    {
        NeedsItems.Clear();
        WantsItems.Clear();
        HasItems.Clear();

        foreach (var item in _stateService.State.Needs)
        {
            NeedsItems.Add(new InventoryItemViewModel(item, InventoryBucket.Needs));
        }

        foreach (var item in _stateService.State.Wants)
        {
            WantsItems.Add(new InventoryItemViewModel(item, InventoryBucket.Wants));
        }

        foreach (var item in _stateService.State.Has)
        {
            HasItems.Add(new InventoryItemViewModel(item, InventoryBucket.Has));
        }
    }

    private void AddItem(string itemName, InventoryBucket bucket)
    {
        var trimmed = itemName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        var model = new InventoryItem
        {
            Name = trimmed,
            Bucket = bucket
        };

        GetStateList(bucket).Add(model);
        GetCollection(bucket).Add(new InventoryItemViewModel(model, bucket));
        Persist();
    }

    private ObservableCollection<InventoryItemViewModel> GetCollection(InventoryBucket bucket)
    {
        return bucket switch
        {
            InventoryBucket.Needs => NeedsItems,
            InventoryBucket.Wants => WantsItems,
            _ => HasItems
        };
    }

    private List<InventoryItem> GetStateList(InventoryBucket bucket)
    {
        return bucket switch
        {
            InventoryBucket.Needs => _stateService.State.Needs,
            InventoryBucket.Wants => _stateService.State.Wants,
            _ => _stateService.State.Has
        };
    }

    private void Persist()
    {
        _stateService.Save();
    }

    [RelayCommand]
    private void OpenPhotoPreview(InventoryItemViewModel? item)
    {
        if (item is null || !item.HasPhoto)
        {
            return;
        }

        PhotoPreviewItem = item;
        PhotoPreviewSource = item.PhotoSource;
        IsPhotoPreviewVisible = true;
    }

    [RelayCommand]
    private void ClosePhotoPreview()
    {
        IsPhotoPreviewVisible = false;
        PhotoPreviewSource = null;
        PhotoPreviewItem = null;
    }

    public void SetItemPhoto(Guid itemId, InventoryBucket bucket, byte[] photoData)
    {
        if (photoData.Length == 0)
        {
            return;
        }

        var item = GetCollection(bucket).FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return;
        }

        item.PhotoData = photoData;

        if (PhotoPreviewItem is not null && PhotoPreviewItem.Id == itemId && PhotoPreviewItem.Bucket == bucket)
        {
            PhotoPreviewSource = item.PhotoSource;
        }

        Persist();
    }
}
