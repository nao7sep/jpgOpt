<!-- 2025-04-01T04:41:56Z -->

# Image Saturation Optimizer Spec v0.1

## Table of Contents
1. [Introduction & Goals](#introduction--goals)
2. [High-Level Overview](#high-level-overview)
3. [App Workflow](#app-workflow)
4. [UI & UX Requirements](#ui--ux-requirements)
5. [Session & Output Management](#session--output-management)
6. [Models & Data Structures](#models--data-structures)
   - [InputImage](#model-inputimage)
   - [OptimizationTask](#model-optimizationtask)
   - [ThumbnailCacheEntry](#model-thumbnailcacheentry)
   - [AppNotification](#model-appnotification--notificationtype)
7. [Image Processing Pipeline](#image-processing-pipeline)
8. [Thumbnail Caching](#thumbnail-caching)
9. [Restoring a Previous Session](#restoring-a-previous-session)
10. [Notification System](#notification-system)
11. [Edge Cases & Special Modes](#edge-cases--special-modes)
12. [Future Considerations](#future-considerations)
13. [Acceptance Criteria](#acceptance-criteria)

---

## 1. Introduction & Goals

This application, built using **Avalonia UI**, allows users to:

1. **Load multiple images** via file dialog or drag-and-drop.
2. **Preview images** as thumbnails and adjust **saturation**.
3. **Queue background optimization tasks** (one at a time) with minimal, natural edits:
   - Auto-orient
   - Transform color space + remove ICC
   - (Optional) Remove GPS or all metadata
   - Saturation adjustment
   - Linear stretch + adaptive sharpen
4. **Output to a timestamped directory** on the user’s desktop, containing optimized JPEGs and a JSON session file.
5. **Reopen/restore** a previous session to continue adjustments or re-run tasks, detecting changed or missing originals.

This spec emphasizes **simplicity**, **performance**, and **clean, minimal editing** of images.

---

## 2. High-Level Overview

1. **Main Window**
   - Displays a thumbnail list of all loaded images.
   - Shows basic controls for saturation.
   - Offers “Optimize” button to queue background tasks.

2. **Output Folder**
   - Created at first optimization, named `jpgOpt-<timestamp>` (e.g., `jpgOpt-20250401T144510Z`).
   - Contains all optimized images + a JSON file (the session metadata).

3. **Data Models**
   - **`InputImage`** describes each original image’s properties, user’s chosen parameters, etc.
   - **`OptimizationTask`** logs queued jobs, the chosen saturation, and metadata removal settings.
   - **`ThumbnailCacheEntry`** for in-memory caching of thumbnails.
   - **`AppNotification`** for background warnings or errors (non-blocking).

4. **No Overengineering**
   - No multi-session management in a single app instance.
   - No advanced undo/redo.
   - Minimal overhead for caching and data persistence.

---

## 3. App Workflow

1. **User Drags and Drops or Selects Files**
   - Images are added to an internal list, ignoring duplicates.
   - Thumbnails are generated in memory.

2. **User Adjusts Saturation**
   - A slider (or numeric input) changes the image’s saturation in real-time on the thumbnail.
   - Other minimal edits (remove GPS/metadata) can be toggled individually.

3. **User Presses “Optimize”**
   - All images marked as “pending” (due to parameter changes) get queued.
   - A background process (one at a time) applies the full Magick.NET pipeline, saving outputs to the timestamped folder.

4. **User Can Modify Parameters Mid-Queue**
   - If the user changes a parameter for an image already in process or processed, that image is re-queued **at the front**.

5. **User Deletes Images**
   - Cancels pending tasks and removes them from the list.
   - Output images already produced are also removed.

6. **App Close**
   - If tasks remain in queue, a confirmation dialog appears.
   - If OS session ends, the background process is terminated gracefully, and the app closes.

---

## 4. UI & UX Requirements

1. **Thumbnail List**
   - Suggestion: Use a scrolling panel with virtualization.
   - Single selection for controlling saturation.
   - Thumbnails update in near real-time as user adjusts saturation.

2. **Saturation Slider**
   - 100% = no change.
   - Range could be 0–200% or 0–300%, as desired.
   - Only enabled if exactly one image is selected.

3. **Metadata Checkboxes**
   - `Remove GPS`
   - `Remove All Metadata`
   - Reflect the user’s preference for each image (persisted in `InputImage`).

4. **Optimize Button**
   - Queues all unprocessed or changed images for background optimization.

5. **Delete Button**
   - Removes the selected image from the list and queue.
   - Removes any output file on disk if it exists.

6. **Notification List**
   - Non-blocking list or pane that accumulates warnings/errors from the background process.
   - Could be collapsible or displayed in a side panel.

7. **Close Behavior**
   - If the queue is active, a confirmation dialog is shown.
   - If the system session ends, the app cancels tasks and closes without prompting.

---

## 5. Session & Output Management

1. **Single Session = Single App Window**
   - No multi-session handling in one instance.
   - The app writes a single JSON for each session in the corresponding output directory.

2. **Output Directory**
   - Named: `jpgOpt-<UTC timestamp in yyyyMMdd'T'HHmmss'Z'>`
     - Example: `jpgOpt-20250401T144510Z`
   - Created on first optimization if it doesn’t exist.
   - Contains all generated JPEGs + `session.json`.

3. **session.json**
   - Holds two top-level lists:
     1. **`InputImages`**
     2. **`OptimizationTasks`**
   - Also contains a **`SessionId`** (a `Guid`) to identify the session.
   - No absolute path to the output dir is saved anywhere; user can rename it freely later.

---

## 6. Models & Data Structures

### Model: `InputImage`
```csharp
public class InputImage
{
    public Guid Id { get; set; } = Guid.NewGuid(); // Unique ID for internal references

    public string FilePath { get; set; }           // Absolute path to the original file
    public long FileLength { get; set; }           // Size in bytes
    public int Width { get; set; }                 // After orientation
    public int Height { get; set; }                // After orientation
    public string QuickHash { get; set; }          // Partial hash to detect modifications
    public DateTime LastModifiedUtc { get; set; }  // From FileInfo

    // Current user-chosen parameters
    public float SaturationAdjustment { get; set; } // E.g., 100 = no change, 120 = +20%
    public bool RemoveGps { get; set; }
    public bool RemoveAllMetadata { get; set; }

    public string? OutputFileName { get; set; }    // Assigned name in output folder
}
```

**Notes**:
- `QuickHash` could be a partial SHA256 of first 1KB + length + last modified time.
- `SaturationAdjustment` is stored here so the UI can display/update the user’s current setting.

### Model: `OptimizationTask`
```csharp
public class OptimizationTask
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Linking field to find the matching InputImage
    public string InputPath { get; set; }           // Matches an InputImage.FilePath
    public string OutputFileName { get; set; }      // Where the processed image will be saved
    public DateTime QueuedAtUtc { get; set; }

    public float SaturationUsed { get; set; }       // Copied from InputImage at queue time
    public bool RemovedGps { get; set; }            // Copied from InputImage
    public bool RemovedAllMetadata { get; set; }    // Copied from InputImage

    public bool Completed { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
```

**Notes**:
- `InputPath` is a string for easier JSON serialization.
- `RemovedGps` / `RemovedAllMetadata` used for historical record of the exact state at processing time.

### Model: `ThumbnailCacheEntry`
```csharp
public class ThumbnailCacheEntry
{
    public Guid ImageId { get; set; }              // Matches InputImage.Id
    public Bitmap Thumbnail { get; set; }          // Avalonia.Media.Imaging.Bitmap
    public DateTime LastUpdatedUtc { get; set; }
}
```

**Notes**:
- In-memory only.
- Could include optional disposal logic for large batch usage.

### Model: `AppNotification` & `NotificationType`
```csharp
public class AppNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
}

public enum NotificationType
{
    Info,
    Warning,
    Error
}
```

**Notes**:
- Stored in an `ObservableCollection<AppNotification>` or similar for UI binding.
- **No dialogs** for background errors; the user sees them in a dedicated notifications list.

---

## 7. Image Processing Pipeline

Using **Magick.NET**, each queued task processes the image as follows:

1. **AutoOrient**
   - Reads EXIF orientation and fixes rotation/flip.

2. **TransformColorSpace**
   - Convert to sRGB or another standard color space.
   - Removes ICC profile once normalized.

3. **Remove ICC Profile**
   - Strips embedded profile post-conversion.

4. **Remove EXIF Thumbnail**
   - Removes any embedded EXIF preview.

5. **Saturation Adjustment**
   - Via `Modulate`, using the `SaturationUsed` from the queued task.
   - 100% = no change.

6. **LinearStretch**
   - Gently adjusts brightness/contrast to a “natural” range.

7. **AdaptiveSharpen**
   - Light sharpening to counter minor softness.

8. **Strip Metadata** (Conditional)
   - If `RemoveAllMetadata` is `true`, call `Strip()` entirely.
   - Otherwise, if `RemoveGps` is `true`, remove GPS tags specifically.

**Output**:
- Saved as a JPEG to `OutputFileName` in the session’s directory.

---

## 8. Thumbnail Caching

- **In-Memory** caching of thumbnails.
- **Re-generated** on each new session load or when the user modifies saturation.
- No disk caching by default.
- Implementation outline:
  ```csharp
  public class ThumbnailCache
  {
      private readonly Dictionary<Guid, ThumbnailCacheEntry> _cache = new();

      public bool TryGet(Guid imageId, out Bitmap? bitmap) { ... }
      public void AddOrUpdate(Guid imageId, Bitmap thumb) { ... }
      public void Remove(Guid imageId) { ... }
      public void Clear() { ... }
  }
  ```

---

## 9. Restoring a Previous Session

When the user opens an **existing output directory** or the app is launched with a JSON file:

1. **Load** `session.json` → retrieve `InputImages` and `OptimizationTasks`.
2. **Check existence** of each file from `InputImages`:
   - If missing → mark the image in **red** or otherwise highlight it as missing.
3. **Compute QuickHash** of each file found on disk:
   - If mismatch → mark the image in **yellow**, user decides to keep or remove.
4. **Check for untracked output files** in the folder (files that the JSON doesn’t reference):
   - Show a **warning dialog** offering to delete them or leave them.
   - No attempt to rebuild JSON for these unknown files.
5. **UI** is updated so user can continue adjusting or re-running tasks as needed.

---

## 10. Notification System

- A **background** queue processes images.
- On any **error** or **warning** (e.g., file locked, Magick exception, unexpected format), an `AppNotification` is created:
  - `Message`
  - `Type` = `Warning` or `Error`
- The UI binds an `ObservableCollection<AppNotification>`.
- The user can open/collapse a **notifications panel** (or list) to review them.
- **No blocking dialogs** from the background process.

---

## 11. Edge Cases & Special Modes

1. **Timestamps in Filenames**
   - Many photos have `20250401_120000_` type prefixes.
   - The user can optionally remove or ignore them.
   - Alternatively, a “filename cleanup” mode might strip them automatically.

2. **Burst/Sequential Suffixes** (`_1`, `_2`, etc.)
   - Could also be stripped automatically if desired.
   - This is optional, not a core requirement.

3. **Output Filename Conflicts**
   - Since each `InputImage` has a unique `OutputFileName`, we allow overwriting any existing file with the same name.
   - No manual file naming required.

4. **Large Batches**
   - The app is primarily designed for ~100 images.
   - If performance becomes an issue, consider virtualization or an LRU cache for thumbnails.

---

## 12. Future Considerations

Below are possible expansions or refinements if the need arises:

1. **Batch UI** for group adjustments of saturation, etc.
2. **Undo/Redo** stack for parameter changes.
3. **Keyboard Shortcuts** for faster image navigation.
4. **Multiple concurrent tasks** (thread pool, careful concurrency).
5. **Presets** for different editing styles (e.g., “Natural,” “Vivid,” “Black & White”).
6. **CLI Mode** for headless batch processing.
7. **Localization/Globalization** for multi-language support.
8. **Metadata Editor** or more granular EXIF manipulation.

---

## 13. Acceptance Criteria

1. **Image Loading & Thumbnail Display**
   - The user can add images via dialog or drag-and-drop.
   - Thumbnails appear quickly and reflect real-time saturation changes.

2. **Saturation & Metadata**
   - `SaturationAdjustment`, `RemoveGps`, `RemoveAllMetadata` are adjustable per image.
   - These settings are persisted in `session.json` and used in optimization tasks.

3. **Background Optimization**
   - Clicking “Optimize” queues tasks that run one-at-a-time.
   - Output images are placed in `jpgOpt-<timestamp>` with a valid session JSON.

4. **Session Restore**
   - `session.json` is loadable.
   - Missing or modified files are flagged.
   - Untracked output images trigger a user decision (delete or keep).

5. **Notifications**
   - Errors and warnings appear in a non-blocking notification list.
   - The user can ignore or view them without halting the background process.

6. **Cleanup**
   - Deleting an image removes it from the queue, the in-memory list, and any existing output file.
   - If tasks are in progress, the user must confirm closing (except on OS session end).

7. **No Crashes with Locked Files**
   - If an image is locked or invalid, a notification is raised — no unhandled exception.
