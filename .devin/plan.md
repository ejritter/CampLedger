# Fix drag-and-drop between Needs/Wants/Has + auto-collapse during drag
## Root cause (confirmed via git history)
`CampLedger/Pages/InventoryPage.xaml` was overwritten by a past commit
(`e3b2d61 "Save uncommitted changes"`, June 25) that stripped out the
`DragGestureRecognizer` / `DropGestureRecognizer` XAML wiring, along with the
Edit-item popup, Photo capture/preview UI, and the manual Expand/Collapse
toggle buttons. The supporting logic for all of this **still fully exists**
and works today, just disconnected from any UI:
- `InventoryViewModel`: `MoveItem`, `EditItem`/`SaveEdit`/`CancelEdit`,
  `SetItemPhoto`/`OpenPhotoPreview`/`ClosePhotoPreview`,
  `IsNeedsExpanded`/`IsWantsExpanded`/`IsHasExpanded` +
  `ToggleXExpandedCommand`.
- `InventoryPage.xaml.cs`: `OnItemDragStarting`, `OnXDropZoneDragOver/Drop`,
  `OnDropZoneDragLeave`, `OnAddPhotoClicked` (MediaPicker/Permissions),
  `OnRemoveItemClicked`.
- SQLite persistence (`CampLedgerStorageService`) already stores
  `PhotoData` as a BLOB column and follows the required
  thread-pool-bridged async pattern (`Task.Run(...).GetAwaiter().GetResult()`
  for `Load`, fire-and-forget chained `Task.Run` for `Save`) — no changes
  needed there.
So this is primarily a **XAML rebuild** for `InventoryPage.xaml`, plus new
behavior (auto-collapse during drag) and two small new Popups.
## Decisions confirmed with user
- Recreate Edit item, Photo capture/preview, and manual Expand/Collapse —
  not just restore the old markup verbatim; rebuild using current MVVM/async/
  SQLite skills.
- During any drag, **all three** lists (including the one you dragged from)
  collapse to header-only, then restore their prior expand state once the
  drag ends (dropped or not).
## Plan
### 1. Tests first (TDD)
- Link `InventoryViewModel.cs`, `InventoryItemViewModel.cs`,
  `ViewModelBase.cs` into `tests/CampLedger.Tests/CampLedger.Tests.csproj`
  (same `<Compile Include ... Link ...>` pattern already used for other
  app source files — this test project doesn't reference the MAUI app
  project directly).
- Add `FakeCampLedgerStateService` test double (in-memory
  `ICampLedgerStateService`).
- New `InventoryViewModelTests.cs` — write failing tests first for:
  - `MoveItem` between buckets (regression coverage for the D&D fix).
  - New `BeginDrag()`/`EndDrag()` collapse + restore (including restoring
    mixed prior states, not just all-true).
  - `EditItem`/`SaveEdit`/`CancelEdit`.
  - `SetItemPhoto`.
  - `ToggleXExpandedCommand`.
  - `Search`/`ApplySearch` filtering (currently untested).
### 2. ViewModel additions (`InventoryViewModel`)
- `BeginDrag()`: snapshot current `IsNeedsExpanded/IsWantsExpanded/IsHasExpanded`,
  then force all three to `false`. Guarded by an `_isDragging` flag (no-op if
  already dragging).
- `EndDrag()`: restore the snapshotted values; clears `_isDragging`.
- `EditRequested` / `PhotoPreviewRequested` events, raised from
  `EditItem`/`OpenPhotoPreview` after state is set — mirrors the existing
  `ThemeSelectionPopupViewModel.CloseRequested` /
  `TripLocationPopupViewModel.LocationSelected` event pattern already used
  in this codebase for View-initiated popup display.
### 3. Two new Popups (CommunityToolkit `Popup`, matching existing
   `ThemeSelectionPopup`/`TripLocationPopupPage` conventions)
- `Pages/InventoryItemEditPopup.xaml(.cs)` — `x:DataType` is the shared
  `InventoryViewModel` (not a new VM); Entry bound to `EditingItemName`,
  Save/Cancel bound to `SaveEditCommand`/`CancelEditCommand`.
- `Pages/InventoryPhotoPreviewPopup.xaml(.cs)` — Image bound to
  `PhotoPreviewSource`, Close bound to `ClosePhotoPreviewCommand`.
- Both constructed directly with the page's existing `InventoryViewModel`
  instance (`new InventoryItemEditPopup(ViewModel)`), shown via
  `await this.GetPresentingPage().ShowPopupAsync(popup)` — same
  direct-construction pattern already used for `TripLocationPopupPage`
  (required here since state like `EditingItem`/`PhotoPreviewItem` must be
  shared with the page's VM instance, not a fresh DI-resolved one).
- Each popup resets the relevant VM state on `Popup.Closed` as a safety net
  (e.g. tap-outside-to-dismiss without Save/Cancel).
- `InventoryPage` constructor subscribes to `EditRequested`/
  `PhotoPreviewRequested` and shows the matching popup.
### 4. Rebuild `InventoryPage.xaml`
- Each section (Needs/Wants/Has) wrapped in an outer `Border` that is the
  **drop zone**: `DropGestureRecognizer AllowDrop="True"` +
  `DragOver`/`DragLeave`/`Drop` → existing code-behind handlers.
- Section header: title Label + Expand/Collapse toggle button bound to
  existing `IsXExpanded`/`XToggleText`/`ToggleXExpandedCommand`.
- Collapsible body (Add-item row + `CollectionView`) wrapped so
  `IsVisible="{Binding IsXExpanded}"` — reused for both manual toggle and
  the new auto-collapse-during-drag behavior.
- Item template root `Border` gets a `DragGestureRecognizer` with
  `DragStarting="OnItemDragStarting"` (existing) and new
  `DropCompleted="OnItemDropCompleted"`.
- Item template adds Edit button (`Command` → `EditItemCommand` via
  `RelativeSource AncestorType=InventoryViewModel`, since `CollectionView`
  breaks direct binding to the page VM), Photo action button (`Clicked=
  "OnAddPhotoClicked"`, existing code-behind, untouched), thumbnail/expand
  bound to `HasPhoto`/`PhotoSource`/`IsPhotoExpanded`/`TogglePhotoCommand`
  with a tap gesture calling `OpenPhotoPreviewCommand`. Remove button stays
  exactly as it is today (`Clicked="OnRemoveItemClicked"` — already works,
  not part of the regression, left untouched for consistency with the
  app's existing confirmation-dialog convention).
### 5. Code-behind (`InventoryPage.xaml.cs`)
- `OnItemDragStarting`: keep existing payload logic, add `ViewModel.BeginDrag()`.
- New `OnItemDropCompleted`: call `ViewModel.EndDrag()` — fires whether the
  item was dropped successfully, dropped nowhere, or the drag was cancelled,
  so lists always re-expand.
- Constructor: subscribe to `ViewModel.EditRequested` /
  `ViewModel.PhotoPreviewRequested` to show the two new popups.
### 6. Verification
- `dotnet build` for `net10.0-windows10.0.19041.0` (baseline already green).
- `dotnet test` (baseline: 21 passed, 0 failed).
- Note for the user: actual on-screen drag-gesture feel (native OS drag
  visuals, collapse timing) should be spot-checked on a running
  Windows/Android build — unit tests cover ViewModel logic only, not gesture
  rendering.
## Notes / risk
- Collapsing the source list's `CollectionView` mid-drag is expected to be
  safe on all platforms because the native drag payload/image is captured
  by the platform drag manager at `DragStarting` time, independent of the
  visual tree afterward — this is the standard technique for this UX, but
  worth a quick manual check on-device since it can't be exercised by an
  automated test in this environment.
- Scope is intentionally limited to Inventory drag-and-drop + the
  associated Edit/Photo/Expand-collapse UI it depends on; no changes to
  Trip Planning/Trip History pages.