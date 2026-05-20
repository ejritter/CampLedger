## New add-on for adding photos to items in needs/has/wants lists
- Follow main `campledger-instructions.md` for overall project requirements.
- This is a new feature added to existing production code.
- Platform priority is Android first. iOS and Windows are supported but secondary.
- Use .NET 10 MAUI guidance for `Microsoft.Maui.Media` (`MediaPicker`).

## Required Android setup (.NET 10 MAUI)
Use `Platforms/Android/AndroidManifest.xml` with these entries:

- Camera capture permission:
  - `<uses-permission android:name="android.permission.CAMERA" />`
- Network permissions already used by app:
  - `<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />`
  - `<uses-permission android:name="android.permission.INTERNET" />`
- Media/file access compatibility for picker support:
  - `<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" android:maxSdkVersion="32" />`
  - `<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" android:maxSdkVersion="32" />`
  - `<uses-permission android:name="android.permission.READ_MEDIA_AUDIO" />`
  - `<uses-permission android:name="android.permission.READ_MEDIA_IMAGES" />`
  - `<uses-permission android:name="android.permission.READ_MEDIA_VIDEO" />`
- Android 11+ package visibility for camera intent resolution:
  - 
    ```xml
    <queries>
      <intent>
        <action android:name="android.media.action.IMAGE_CAPTURE" />
      </intent>
    </queries>
    ```

## Runtime permission and capture behavior
- Request camera permission before capture:
  - `Permissions.CheckStatusAsync<Permissions.Camera>()`
  - `Permissions.RequestAsync<Permissions.Camera>()` when needed.
- If permission is denied, show a clear prompt and stop capture flow.
- Use `MediaPicker.Default.IsCaptureSupported` before `CapturePhotoAsync()`.
- If capture is unavailable or unsupported, provide fallback to `PickPhotoAsync()`.
- Use `photo.OpenReadAsync()` to read the selected/captured image stream.
- Keep media picker calls on the UI thread (page event handlers are valid).

## Feature functionality requirements
- Each listed item should show a clear image action symbol/button.
- Clicking symbol:
  - if no image exists, open camera flow.
  - if image exists, prompt to replace before continuing.
- After capture/pick, user confirms whether to keep image or take/select a new one.
- Image display is collapsed by default to save space.
- User can toggle collapse/uncollapse with explicit button.
- When uncollapsed, display image at approximately 50% size.
- User can tap image to open fullscreen preview.
- User can close fullscreen preview and return to default view.
- Trip Ledger page for any Has items should also be able to show the photo attatched to Has item.
- Trip Ledger Page is not allowed to alter that image, only view.
- History page should also have read only access to image. 

## Implementation notes for this repository
- Store photo data with the inventory item model/view model.
- Persist photo updates through existing inventory persistence flow.
- Preserve existing add/edit/remove/search/drag-drop behavior.
- Keep UX and styling consistent with current MAUI page patterns.