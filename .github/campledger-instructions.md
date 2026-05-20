# CampLedger Instruction File

This instruction file defines how Copilot should generate a **.NET 10 MAUI** application named **CampLedger**.
Use SLNX for the Solution files.

It contains no implementation code and serves strictly as a behavioral and structural guide.

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

- Use Microsoft’s latest FluentUI design for all UI elements.
- Use native MAUI XAML controls only.
- Use CommunityToolkit.Mvvm ObservableProperty, ObservableObject and the like for MVVM pattern.
- Always put interfaces, classes, and all types in their own separate files. Never co-locate multiple types in the same file. Use single file-scoped namespaces per file.
- Do not use FillAndExpand in HorizontalOptions. It is deprecated.
- Avoid deprecated methods.
- Use CommunityToolkit.Mvvm source generators with partial properties and [RelayCommand] methods; avoid manual property bodies and ICommand properties in view models.
- Do not use manual OnPropertyChanged() calls in view models; use CommunityToolkit.Mvvm attributes like ObservableProperty and NotifyPropertyChangedFor.
- Never use [ObservableProperty] with a private backing field (e.g., `private DateTime filterStartDate`). Always use the partial property syntax: `[ObservableProperty] public partial Type PropertyName { get; set; } = defaultValue;`
- Do not use expression-bodied members or properties in this repository; use block-bodied properties and methods instead.
- Folder structure should be `Application/Services`, `Application/Pages`, `Application/ViewModels`, `Application/Views`, `Application/Models`
- ContentPages for ViewModels should be added under `Applicaiton/Pages`.
- Avoid all external UI component libraries (for example: Syncfusion, Telerik).
- Use flyout menus bottom tab for navigation between pages.
- Use these colors for app:
	1. Greens (Use as the main)
	These are approximate because gradients blend multiple tones:
		• #0A5C3B — dark forest green 
		• #1FAE6A — mid‑tone green
		• #4ED08A — bright mint‑green transition
		• #A8F2C8 — pale mint highlight 
	2. White (Secondary/Tetary)
		• #FFFFFF 
	3. Black / Near‑Black(Secondary/Tetary)
	#000000 
## Data Storage Requirements

### 4. Local Storage

The application must:

- Use MAUI Preferences for all local data persistence.
- Store list contents for Needs, Wants, and Has.
- Store trip planning data.
- Store completed trip history.

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

## Behavioral Requirements for Copilot

### 7. Copilot Generation Rules

When generating the CampLedger application, Copilot must:

- Follow this instruction file strictly.
- Generate only .NET 10 MAUI-compatible structures.
- Use MVVM patterns if needed, adhering to CommunityToolkit.Mvvm, use this nuget package.
- Use `[ObservableProperty]`, `[NotifyPropertyChangedFor]`, and `[RelayCommand]` for view model properties and commands.
- Do not use manual `OnPropertyChanged()` calls in view models.
- Do not use manual `INotifyPropertyChanged` implementations.
- Avoid generating any code not aligned with these requirements.
- Ensure all UI follows FluentUI guidelines.
- Ensure all storage uses MAUI Preferences.

## Non-Functional Requirements

### 8. Performance and Experience

The application must:

- Load instantly with persisted data.
- Provide smooth drag-and-drop interactions.
- Maintain a clean, modern FluentUI aesthetic.
- Be intuitive for users familiar with checklist apps.

## Summary

CampLedger is a structured, FluentUI-styled .NET 10 MAUI application that:

- Manages camping inventory across Needs, Wants, and Has lists.
- Supports drag-and-drop between lists.
- Uses MAUI Preferences for all storage.
- Includes a full trip-planning system.
- Tracks and displays historical camping trips.