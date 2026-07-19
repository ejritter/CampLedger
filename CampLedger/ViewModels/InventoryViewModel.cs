using System.Collections.ObjectModel;
using CampLedger.Models;
using CampLedger.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampLedger.ViewModels;

public sealed partial class InventoryViewModel : ViewModelBase
{
    private readonly ICampLedgerStateService _stateService;
    private bool _isDragging;
    private bool _snapshottedIsNeedsExpanded;
    private bool _snapshottedIsWantsExpanded;
    private bool _snapshottedIsHasExpanded;
    private InventoryItemViewModel? _draggedItem;

    public event EventHandler? EditRequested;
    public event EventHandler? EditDismissRequested;
    public event EventHandler? PhotoPreviewRequested;
    public event EventHandler? PhotoPreviewDismissRequested;


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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNeedsDragSource))]
    [NotifyPropertyChangedFor(nameof(IsWantsDragSource))]
    [NotifyPropertyChangedFor(nameof(IsHasDragSource))]
    public partial InventoryBucket? DragSourceBucket { get; set; }

    public InventoryViewModel(ICampLedgerStateService stateService)
    {
        _stateService = stateService;

        AllNeedsItems = new ObservableCollection<InventoryItemViewModel>(_stateService.State.Needs.Select(i => new InventoryItemViewModel(i, InventoryBucket.Needs)));
        AllWantsItems = new ObservableCollection<InventoryItemViewModel>(_stateService.State.Wants.Select(i => new InventoryItemViewModel(i, InventoryBucket.Wants)));
        AllHasItems = new ObservableCollection<InventoryItemViewModel>(_stateService.State.Has.Select(i => new InventoryItemViewModel(i, InventoryBucket.Has)));

        NeedsItems = new ObservableCollection<InventoryItemViewModel>(AllNeedsItems);
        WantsItems = new ObservableCollection<InventoryItemViewModel>(AllWantsItems);
        HasItems = new ObservableCollection<InventoryItemViewModel>(AllHasItems);
    }

    public ObservableCollection<InventoryItemViewModel> AllNeedsItems { get; }

    public ObservableCollection<InventoryItemViewModel> AllWantsItems { get; }

    public ObservableCollection<InventoryItemViewModel> AllHasItems { get; }

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

    public bool IsNeedsDragSource
    {
        get
        {
            return DragSourceBucket == InventoryBucket.Needs;
        }
    }

    public bool IsWantsDragSource
    {
        get
        {
            return DragSourceBucket == InventoryBucket.Wants;
        }
    }

    public bool IsHasDragSource
    {
        get
        {
            return DragSourceBucket == InventoryBucket.Has;
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
        model.Bucket = toBucket;
        GetStateList(toBucket).Add(model);

        var fromAllCollection = GetAllCollection(fromBucket);
        var itemVm = fromAllCollection.FirstOrDefault(i => i.Id == itemId) ?? new InventoryItemViewModel(model, toBucket);

        fromAllCollection.Remove(itemVm);
        GetCollection(fromBucket).Remove(itemVm);

        itemVm.Bucket = toBucket;
        itemVm.PhotoData = model.PhotoData;
        AddToAllCollection(toBucket, itemVm);
        AddToVisibleCollection(toBucket, itemVm);
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

        GetAllCollection(item.Bucket).Remove(item);
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
        Persist();
    }

    [RelayCommand]
    private void Search(string? query)
    {
        SearchQuery = query ?? string.Empty;

        if (string.IsNullOrWhiteSpace(query))
        {
            RefreshAllItems();
            return;
        }

        ApplySearch(query);
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
        EditRequested?.Invoke(this, EventArgs.Empty);
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
        EditDismissRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshAllItems()
    {
        NeedsItems.Clear();
        WantsItems.Clear();
        HasItems.Clear();

        foreach (var item in AllNeedsItems)
        {
            NeedsItems.Add(item);
        }

        foreach (var item in AllWantsItems)
        {
            WantsItems.Add(item);
        }

        foreach (var item in AllHasItems)
        {
            HasItems.Add(item);
        }
    }

    private void ApplySearch(string? query)
    {
        NeedsItems.Clear();
        WantsItems.Clear();
        HasItems.Clear();

        var lowerQuery = query?.Trim();
        if (string.IsNullOrWhiteSpace(lowerQuery))
        {
            RefreshAllItems();
            return;
        }

        AddFilteredItems(AllNeedsItems, NeedsItems, lowerQuery);
        AddFilteredItems(AllWantsItems, WantsItems, lowerQuery);
        AddFilteredItems(AllHasItems, HasItems, lowerQuery);
    }

    private static void AddFilteredItems(IEnumerable<InventoryItemViewModel> source, ObservableCollection<InventoryItemViewModel> target, string query)
    {
        foreach (var item in source)
        {
            if (item.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                target.Add(item);
            }
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

    private ObservableCollection<InventoryItemViewModel> GetAllCollection(InventoryBucket bucket)
    {
        return bucket switch
        {
            InventoryBucket.Needs => AllNeedsItems,
            InventoryBucket.Wants => AllWantsItems,
            _ => AllHasItems
        };
    }

    private void AddToAllCollection(InventoryBucket bucket, InventoryItemViewModel item)
    {
        GetAllCollection(bucket).Add(item);
    }

    private void AddToVisibleCollection(InventoryBucket bucket, InventoryItemViewModel item)
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            GetCollection(bucket).Add(item);
            return;
        }

        if (item.Name.Contains(SearchQuery.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            GetCollection(bucket).Add(item);
        }
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
        PhotoPreviewRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ClosePhotoPreview()
    {
        IsPhotoPreviewVisible = false;
        PhotoPreviewSource = null;
        PhotoPreviewItem = null;
        PhotoPreviewDismissRequested?.Invoke(this, EventArgs.Empty);
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

    public void BeginDrag(InventoryItemViewModel? item)
    {
        if (_isDragging || item is null)
        {
            return;
        }

        _snapshottedIsNeedsExpanded = IsNeedsExpanded;
        _snapshottedIsWantsExpanded = IsWantsExpanded;
        _snapshottedIsHasExpanded = IsHasExpanded;

        IsNeedsExpanded = false;
        IsWantsExpanded = false;
        IsHasExpanded = false;

        _draggedItem = item;
        _draggedItem.IsBeingDragged = true;
        DragSourceBucket = item.Bucket;
        _isDragging = true;
    }

    public void EndDrag()
    {
        if (!_isDragging)
        {
            return;
        }

        IsNeedsExpanded = _snapshottedIsNeedsExpanded;
        IsWantsExpanded = _snapshottedIsWantsExpanded;
        IsHasExpanded = _snapshottedIsHasExpanded;

        if (_draggedItem is not null)
        {
            _draggedItem.IsBeingDragged = false;
            _draggedItem = null;
        }

        DragSourceBucket = null;
        _isDragging = false;
    }
}
