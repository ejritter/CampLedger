using CampLedger.Models;
using CampLedger.Tests.TestDoubles;
using CampLedger.ViewModels;
using Xunit;

namespace CampLedger.Tests;

public sealed class InventoryViewModelTests
{
    [Fact]
    public void MoveItem_MovesItemBetweenBuckets()
    {
        // Arrange
        var item = new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs };
        var state = new CampLedgerState
        {
            Needs = [item],
            Wants = [],
            Has = [],
            CurrentTrip = null!,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        var itemId = item.Id;

        // Act
        viewModel.MoveItem(itemId, InventoryBucket.Needs, InventoryBucket.Wants);

        // Assert
        Assert.Empty(viewModel.NeedsItems);
        Assert.Single(viewModel.WantsItems);
        Assert.Equal("Tent", viewModel.WantsItems[0].Name);
        Assert.Equal(InventoryBucket.Wants, viewModel.WantsItems[0].Bucket);
    }

    [Fact]
    public void MoveItem_DoesNothingWhenFromAndToBucketAreSame()
    {
        // Arrange
        var item = new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs };
        var state = new CampLedgerState
        {
            Needs = [item],
            Wants = [],
            Has = [],
            CurrentTrip = null!,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        var itemId = item.Id;
        var originalNeedsCount = viewModel.NeedsItems.Count;

        // Act
        viewModel.MoveItem(itemId, InventoryBucket.Needs, InventoryBucket.Needs);

        // Assert
        Assert.Equal(originalNeedsCount, viewModel.NeedsItems.Count);
        Assert.Empty(viewModel.WantsItems);
        Assert.Empty(viewModel.HasItems);
    }

    [Fact]
    public void BeginDrag_CollapsesAllSectionsAndSnapshotsState()
    {
        // Arrange
        var state = new CampLedgerState
        {
            Needs = [new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs }],
            Wants = [],
            Has = [],
            CurrentTrip = null!,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        // Set initial state with mixed expand states
        viewModel.IsNeedsExpanded = true;
        viewModel.IsWantsExpanded = false;
        viewModel.IsHasExpanded = true;

        // Act
        viewModel.BeginDrag(viewModel.NeedsItems[0]);

        // Assert
        Assert.False(viewModel.IsNeedsExpanded);
        Assert.False(viewModel.IsWantsExpanded);
        Assert.False(viewModel.IsHasExpanded);
        Assert.True(viewModel.IsNeedsDragSource);
        Assert.False(viewModel.IsWantsDragSource);
        Assert.False(viewModel.IsHasDragSource);
        Assert.True(viewModel.NeedsItems[0].IsBeingDragged);
    }

    [Fact]
    public void EndDrag_RestoresSnapshottedExpandStates()
    {
        // Arrange
        var state = new CampLedgerState
        {
            Needs = [new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs }],
            Wants = [],
            Has = [],
            CurrentTrip = null!,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        // Set initial state with mixed expand states
        viewModel.IsNeedsExpanded = true;
        viewModel.IsWantsExpanded = false;
        viewModel.IsHasExpanded = true;

        // Act
        viewModel.BeginDrag(viewModel.NeedsItems[0]);
        viewModel.EndDrag();

        // Assert
        Assert.True(viewModel.IsNeedsExpanded);
        Assert.False(viewModel.IsWantsExpanded);
        Assert.True(viewModel.IsHasExpanded);
        Assert.Null(viewModel.DragSourceBucket);
        Assert.False(viewModel.NeedsItems[0].IsBeingDragged);
    }

    [Fact]
    public void BeginDrag_WhenAlreadyDragging_DoesNothing()
    {
        // Arrange
        var state = new CampLedgerState
        {
            Needs = [new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs }],
            Wants = [],
            Has = [],
            CurrentTrip = null!,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        viewModel.IsNeedsExpanded = true;
        viewModel.IsWantsExpanded = true;
        viewModel.IsHasExpanded = true;

        // Act
        viewModel.BeginDrag(viewModel.NeedsItems[0]);
        // Change states while dragging
        viewModel.IsNeedsExpanded = false;
        viewModel.IsWantsExpanded = false;
        viewModel.IsHasExpanded = false;
        // Try to begin drag again
        viewModel.BeginDrag(viewModel.NeedsItems[0]);

        // Assert - should still be collapsed from first BeginDrag
        Assert.False(viewModel.IsNeedsExpanded);
        Assert.False(viewModel.IsWantsExpanded);
        Assert.False(viewModel.IsHasExpanded);
    }

    [Fact]
    public void EditItem_SetsEditingItemAndEditingItemName()
    {
        // Arrange
        var item = new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs };
        var state = new CampLedgerState
        {
            Needs = [item],
            Wants = [],
            Has = [],
            CurrentTrip = null!,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        var itemViewModel = viewModel.NeedsItems[0];

        // Act
        viewModel.EditItemCommand.Execute(itemViewModel);

        // Assert
        Assert.NotNull(viewModel.EditingItem);
        Assert.Equal(itemViewModel.Id, viewModel.EditingItem.Id);
        Assert.Equal("Tent", viewModel.EditingItemName);
        Assert.True(viewModel.IsEditing);
    }

    [Fact]
    public void SaveEdit_UpdatesItemNameAndClearsEditingState()
    {
        // Arrange
        var item = new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs };
        var state = new CampLedgerState
        {
            Needs = [item],
            Wants = [],
            Has = [],
            CurrentTrip = null!,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        var itemViewModel = viewModel.NeedsItems[0];
        viewModel.EditItemCommand.Execute(itemViewModel);
        viewModel.EditingItemName = "Camping Tent";

        // Act
        viewModel.SaveEditCommand.Execute(null);

        // Assert
        Assert.Null(viewModel.EditingItem);
        Assert.Empty(viewModel.EditingItemName);
        Assert.False(viewModel.IsEditing);
        Assert.Equal("Camping Tent", viewModel.NeedsItems[0].Name);
    }

    [Fact]
    public void SaveEdit_WhenNameIsEmpty_CancelsEdit()
    {
        // Arrange
        var item = new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs };
        var state = new CampLedgerState
        {
            Needs = [item],
            Wants = [],
            Has = [],
            CurrentTrip = null!,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        var itemViewModel = viewModel.NeedsItems[0];
        viewModel.EditItemCommand.Execute(itemViewModel);
        viewModel.EditingItemName = "   ";

        // Act
        viewModel.SaveEditCommand.Execute(null);

        // Assert
        Assert.Null(viewModel.EditingItem);
        Assert.Empty(viewModel.EditingItemName);
        Assert.False(viewModel.IsEditing);
        Assert.Equal("Tent", viewModel.NeedsItems[0].Name); // Name unchanged
    }

    [Fact]
    public void CancelEdit_ClearsEditingStateWithoutChangingName()
    {
        // Arrange
        var item = new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs };
        var state = new CampLedgerState
        {
            Needs = [item],
            Wants = [],
            Has = [],
            CurrentTrip = null!,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        var itemViewModel = viewModel.NeedsItems[0];
        viewModel.EditItemCommand.Execute(itemViewModel);
        viewModel.EditingItemName = "Camping Tent";

        // Act
        viewModel.CancelEditCommand.Execute(null);

        // Assert
        Assert.Null(viewModel.EditingItem);
        Assert.Empty(viewModel.EditingItemName);
        Assert.False(viewModel.IsEditing);
        Assert.Equal("Tent", viewModel.NeedsItems[0].Name); // Name unchanged
    }

    [Fact]
    public void SetItemPhoto_UpdatesItemPhotoData()
    {
        // Arrange
        var item = new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs };
        var state = new CampLedgerState
        {
            Needs = [item],
            Wants = [],
            Has = [],
            CurrentTrip = null!,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        var photoData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var itemViewModel = viewModel.NeedsItems[0];

        // Act
        viewModel.SetItemPhoto(itemViewModel.Id, itemViewModel.Bucket, photoData);

        // Assert
        Assert.NotNull(itemViewModel.PhotoData);
        Assert.Equal(photoData, itemViewModel.PhotoData);
        Assert.True(itemViewModel.HasPhoto);
    }

    [Fact]
    public void OpenPhotoPreview_SetsPhotoPreviewStateAndRaisesEvent()
    {
        // Arrange
        var item = new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs };
        var state = new CampLedgerState
        {
            Needs = [item],
            Wants = [],
            Has = [],
            CurrentTrip = null!,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        var photoData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var itemViewModel = viewModel.NeedsItems[0];
        viewModel.SetItemPhoto(itemViewModel.Id, itemViewModel.Bucket, photoData);

        var eventRaised = false;
        viewModel.PhotoPreviewRequested += (s, e) => eventRaised = true;

        // Act
        viewModel.OpenPhotoPreviewCommand.Execute(itemViewModel);

        // Assert
        Assert.True(eventRaised);
        Assert.True(viewModel.IsPhotoPreviewVisible);
        Assert.NotNull(viewModel.PhotoPreviewItem);
        Assert.Equal(itemViewModel.Id, viewModel.PhotoPreviewItem.Id);
    }

    [Fact]
    public void OpenPhotoPreview_RaisesPhotoPreviewRequestedEvent()
    {
        // Arrange
        var item = new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs };
        var state = new CampLedgerState
        {
            Needs = [item],
            Wants = [],
            Has = [],
            CurrentTrip = null!,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        var photoData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var itemViewModel = viewModel.NeedsItems[0];
        viewModel.SetItemPhoto(itemViewModel.Id, itemViewModel.Bucket, photoData);

        var eventRaised = false;
        viewModel.PhotoPreviewRequested += (s, e) => eventRaised = true;

        // Act
        viewModel.OpenPhotoPreviewCommand.Execute(itemViewModel);

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void ToggleNeedsExpanded_TogglesIsNeedsExpanded()
    {
        // Arrange
        var state = new CampLedgerState
        {
            Needs = [],
            Wants = [],
            Has = [],
            CurrentTrip = null!,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        var initialState = viewModel.IsNeedsExpanded;

        // Act
        viewModel.ToggleNeedsExpandedCommand.Execute(null);

        // Assert
        Assert.NotEqual(initialState, viewModel.IsNeedsExpanded);
    }

    [Fact]
    public void ToggleWantsExpanded_TogglesIsWantsExpanded()
    {
        // Arrange
        var state = new CampLedgerState
        {
            Needs = [],
            Wants = [],
            Has = [],
            CurrentTrip = null!,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        var initialState = viewModel.IsWantsExpanded;

        // Act
        viewModel.ToggleWantsExpandedCommand.Execute(null);

        // Assert
        Assert.NotEqual(initialState, viewModel.IsWantsExpanded);
    }

    [Fact]
    public void ToggleHasExpanded_TogglesIsHasExpanded()
    {
        // Arrange
        var state = new CampLedgerState
        {
            Needs = [],
            Wants = [],
            Has = [],
            CurrentTrip = null!,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        var initialState = viewModel.IsHasExpanded;

        // Act
        viewModel.ToggleHasExpandedCommand.Execute(null);

        // Assert
        Assert.NotEqual(initialState, viewModel.IsHasExpanded);
    }

    [Fact]
    public void Search_WithQuery_FiltersItems()
    {
        // Arrange
        var state = new CampLedgerState
        {
            Needs =
            [
                new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs },
                new InventoryItem { Name = "Water Bottle", Bucket = InventoryBucket.Needs },
                new InventoryItem { Name = "Sleeping Bag", Bucket = InventoryBucket.Needs }
            ],
            Wants = [],
            Has = [],
            CurrentTrip = null,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        // Act
        viewModel.SearchCommand.Execute("tent");

        // Assert
        Assert.Single(viewModel.NeedsItems);
        Assert.Equal("Tent", viewModel.NeedsItems[0].Name);
    }

    [Fact]
    public void Search_WithEmptyQuery_RestoresAllItems()
    {
        // Arrange
        var state = new CampLedgerState
        {
            Needs =
            [
                new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs },
                new InventoryItem { Name = "Water Bottle", Bucket = InventoryBucket.Needs }
            ],
            Wants = [],
            Has = [],
            CurrentTrip = null,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        // First apply a search
        viewModel.SearchCommand.Execute("tent");
        Assert.Single(viewModel.NeedsItems);

        // Act
        viewModel.SearchCommand.Execute("");

        // Assert
        Assert.Equal(2, viewModel.NeedsItems.Count);
    }

    [Fact]
    public void Search_IsCaseInsensitive()
    {
        // Arrange
        var state = new CampLedgerState
        {
            Needs =
            [
                new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs },
                new InventoryItem { Name = "Water Bottle", Bucket = InventoryBucket.Needs }
            ],
            Wants = [],
            Has = [],
            CurrentTrip = null,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        // Act
        viewModel.SearchCommand.Execute("TENT");

        // Assert
        Assert.Single(viewModel.NeedsItems);
        Assert.Equal("Tent", viewModel.NeedsItems[0].Name);
    }

    [Fact]
    public void Search_WithWhitespace_RestoresAllItems()
    {
        // Arrange
        var state = new CampLedgerState
        {
            Needs =
            [
                new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs },
                new InventoryItem { Name = "Water Bottle", Bucket = InventoryBucket.Needs }
            ],
            Wants = [],
            Has = [],
            CurrentTrip = null,
            TripHistory = []
        };
        var fakeStateService = new FakeCampLedgerStateService(state);
        var viewModel = new InventoryViewModel(fakeStateService);

        // First apply a search
        viewModel.SearchCommand.Execute("tent");
        Assert.Single(viewModel.NeedsItems);

        // Act
        viewModel.SearchCommand.Execute("   ");

        // Assert
        Assert.Equal(2, viewModel.NeedsItems.Count);
    }
}
