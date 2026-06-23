# Copilot Instructions

This instruction file defines how Copilot should generate a **.NET 10 MAUI** application named **CampLedger**.
Use SLNX for the Solution files.

It contains no implementation code and serves strictly as a behavioral and structural guide.

- use copilot skill `C:\Users\roija\.copilot\skills\maui-mvvm-development`
- use copilot skill `C:\Users\roija\.copilot\skills\test-driven-development-maui`
## Project Guidelines
- When implementing Trip Location popup behavior, do not include an external search bar; searching must be done only inside the Google Maps WebView, following .github/TripLedger/TripLocation/trip-location-instructions.md.

## Application Purpose

CampLedger is a camping-focused checklist and trip-planning application. It functions similarly to a to-do list app, tailored for camping preparation, inventory management, and trip history tracking.

## Core Requirements

### 1. Three Primary Lists

The application must include three distinct list sections:

- **Needs** — items required for camping but not yet acquired.
- **Wants** — optional or nice-to-have items.
- **Has / Have** — items the user already owns.

Each list must:

- Support adding items.
- Support removing items.
- Support editing items.
- Support general search of all items and where it is located currently.
- Support long-press interactions.
- Support drag-and-drop interactions.
- Allow dragging items between any of the three lists.

**Example:** dragging an item from **Needs** to **Has**.
**Example:** Search bug spray it is in has list, but i need to refill. So I can just drag it to Needs.

### 2. Drag-and-Drop Interaction

The app must support:

- Long-press gesture to initiate drag.
- Visual feedback during drag.
- Dropping into any other list.
- Automatic list updates after drop.

## UI and Design Requirements

### 3. UI Framework and Style

The application must:
- Use Flyout for `Pages`.
   - FlyoutBehavior is locked. Only display 5% of left side of screen.
   - `TripLedgerPage`,`TripHistoryPage`,`InventoryPage` should be represented in the `FlyoutItems`.
	 - FlyoutItem FlyoutDisplayOptions = AsMultipleItems
		- Each page should be a <ShellContent>
	 - Use `Segoe Font Fluent Icons` from `maui-ui-development` skill for these pages.
   - Have `Theme` Gear always at the bottom as a `Footer`
   - Use `Segoe Font Fluent Icons` from `maui-ui-development` skill for the gear icon.
- ViewModels should be registered as 
## Data Storage Requirements

### 4. Local Storage

The application must:

- Use SQLite for local data persistence via sqlite-net-pcl.
- Store list contents for Needs, Wants, and Has.
- Store trip planning data.
- Store completed trip history.
- Keep the database file in the MAUI app data directory for cross-platform compatibility.

## Trip Planning System

### 5. Trip Planning Section

The application must include a dedicated section for planning camping trips.

This section must:

- Automatically pull items from the Has list.
- Display these items as a packing checklist.
- Have a Packed list
- Have an Unpacked list
- Allow the user to check off items as they pack them.
- checked items move to respected list
- Show any items not yet packed.

**Example:** If the user has a tent in the Has list but has not checked it off for the current trip, the trip planner must show that the tent in Unpacked. User taps it and it moves to Packed. Taps from packed and mvoes to unpacked.

### 6. Saving and Viewing Trips

The application must:

- Allow saving a trip plan.
- Allow updating a saved trip.
- Provide a section to view all previously completed trips.
- Display trip details, including:
  - Items packed
  - Items forgotten
  - Date of trip
  - Any notes the user added

### 7. Performance and Experience

The application must:

- Load instantly with persisted data.
- Provide smooth drag-and-drop interactions.
- Maintain a clean, modern FluentUI aesthetic.
- Be intuitive for users familiar with checklist apps.
- adhere to best practices for async/await coding. 
- not jitter or lag when taking\adding photos of items.

## Summary

CampLedger is a structured, FluentUI-styled .NET 10 MAUI application that:

- Manages camping inventory across Needs, Wants, and Has lists.
- Supports drag-and-drop between lists.
- Uses MAUI Preferences for all storage.
- Includes a full trip-planning system.
- Tracks and displays historical camping trips.