<!-- nao7sep | o1 & o3-mini-high | 2025-04-01T17:45:03Z -->

# Image Saturation Optimizer Spec v0.3

- **Author:** nao7sep
- **Last Updated:** 2025-04-02
- **Version:** 0.3

---

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
13. [Application Configuration, Logging, and Parallel Processing](#application-configuration-logging-and-parallel-processing)
14. [Acceptance Criteria](#acceptance-criteria)

---

## 1. Introduction & Goals

This application, built using **Avalonia UI**, allows users to:
1. **Load multiple images** via file dialog or drag-and-drop.
2. **Preview images** as thumbnails and adjust various parameters including saturation and linear stretch.
3. **Adjust Image Parameters** in the main window using the following controls:
   - **Linear Stretch**: Provides sliders for setting black point and white point percentages. Changes here update thumbnails immediately.
   - **Saturation Slider**: Adjusts saturation in real-time.
   - **Adaptive Sharpen**: A simple toggle (default off) to apply sharpening; does not update thumbnails since its effect is less visible in previews.
   - **GPS and Metadata Removal**: Checkboxes to remove GPS information or all metadata.
4. **Queue background optimization tasks** with a clearly triggered "Optimize" button. This button is critical to initiate processing only after a review phase where optimal parameters are determined, ensuring that CPU-intensive work is not prematurely triggered.
5. **Output to a timestamped directory** on the user’s desktop, containing optimized JPEGs and a JSON session file.
6. **Reopen/restore** a previous session to continue adjustments or re-run tasks, detecting changes or missing original files.

*Key emphasis*: Optimization is triggered by an explicit button press because users must first review optimal parameters for all images; this helps manage CPU load. Additionally, when "Optimize" is pressed, every image is processed—even if its parameters have not changed—because a single, uniform JPEG quality is applied and metadata may be modified, eliminating any need for mere copying of original files.

---

## 2. High-Level Overview

1. **Main Window**
   - Displays a thumbnail list of all loaded images.
   - Shows expanded controls for linear stretch, saturation, adaptive sharpen, and GPS and metadata removal.
   - The order of controls is critical: linear stretch controls (black &amp; white point) appear first, followed by saturation, then adaptive sharpen, and finally GPS and metadata removal.
   - Thumbnail updates occur in real-time when linear stretch or saturation is modified (adaptive sharpen changes do not update thumbnails).

2. **Output Folder**
   - Created at first optimization, named `jpgOpt-<timestamp>` (e.g., `jpgOpt-20250401T144510Z`).
   - Contains all optimized images and a JSON session file.

3. **Data Models**
   - **`InputImage`**: Captures each original image’s properties and current editing parameters.
   - **`OptimizationTask`**: Logs queued jobs, including the selected parameters at queue time.
   - **`ThumbnailCacheEntry`**: For in-memory caching of thumbnails.
   - **`AppNotification`**: Records warnings or errors. Notifications are stored in both the app’s UI and the session JSON.

4. **Processing Behavior**
   - JPEG quality is set with a global control before optimization and becomes immutable once any image has been processed. This is enforced by checking for valid log entries.
   - Rescheduled images (those adjusted mid-queue) are added to the front of the queue so that immediate corrections can be applied.
   - When parameters are adjusted for an image already in the queue: if it is currently processing, a new task is added; if pending, parameters are updated.

---

## 3. App Workflow

1. **Image Loading**
   - Users add images via drag-and-drop or file dialog. Duplicates are ignored and thumbnails are generated immediately.

2. **Parameter Adjustment**
   - Users modify parameters using sliders and toggles:
     - Linear stretch parameters (black and white point percentages)
     - Saturation via slider
     - Adaptive sharpen toggle
     - GPS and Metadata Removal
   - Changes (except for adaptive sharpen) update thumbnails in real-time.

3. **Optimization Trigger**
   - Users set a global JPEG quality before pressing the "Optimize" button.
   - Upon "Optimize" press, every image (even if parameters haven’t changed) is processed to apply the selected JPEG quality and metadata updates.
   - The process checks if any image has already been optimized to lock the JPEG quality value permanently.

4. **Task Management**
   - If an image's parameters are changed while it is in the queue:
     - If currently processing, a new task is appended to the front of the queue.
     - If pending, its parameters are updated in place.
   - This front-queue addition ensures that problematic images can be quickly reprocessed.

5. **Session Closure**
   - On app close, if there are pending tasks, a confirmation is required.
   - In the event of an OS session termination, tasks are terminated gracefully.

---

## 4. UI & UX Requirements

1. **Thumbnail List**
   - Uses a scrolling panel with virtualization. Single selection allows parameter adjustments.

2. **Linear Stretch Control**
   - Two sliders for Black Point Percentage and White Point Percentage.
   - Updates thumbnail preview immediately, ensuring accurate brightness and contrast checks.

3. **Saturation Slider**
   - Adjusts saturation (100% = no change). Range flexible (e.g., 0–200%).
   - Only enabled when exactly one image is selected.

4. **Adaptive Sharpen Toggle**
   - Simple yes/no control, default is off. Its adjustments do not impact thumbnail previews.

5. **Metadata Checkboxes**
   - Options to remove GPS or all metadata, reflecting each image’s settings.

6. **JPEG Quality Control**
   - Set as a single value before optimization; becomes immutable after processing starts.

7. **Optimize Button**
   - Explicitly queues images for optimization.
   - Emphasizes that even unchanged images are processed to apply uniform JPEG quality and metadata modifications.

8. **Delete Button**
   - Cancels pending tasks and removes images from both the display and disk outputs.

9. **Notification Panel**
   - Non-blocking pane displaying errors and warnings, which are also recorded in session JSON.

10. **Control Order**
    - Controls appear in the following order: Linear Stretch, Saturation, Adaptive Sharpen, and then GPS and Metadata Removal.

11. **User Guidance**
    - Clear explanation provided for why optimization is triggered manually, to allow for a review phase before heavy processing begins.

---

## 5. Session & Output Management

1. **Session Handling**
   - Each application run constitutes a single session with one main window.
   - Session data is stored in `session.json` within the output directory and now includes a notifications list along with `InputImages` and `OptimizationTasks`.

2. **Output Structure**
   - Directory named `jpgOpt-<UTC timestamp in yyyyMMdd'T'HHmmss'Z'>` is created on first optimization.
   - All processed images and session data are stored within this folder.

3. **Session File (`session.json`)**
   - Contains lists for `InputImages`, `OptimizationTasks`, and notifications.
   - Identified by a unique `SessionId` (GUID).
   - No absolute file paths are stored.

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
    public float SaturationAdjustment { get; set; } // 100 = no change, 120 = +20%
    public bool RemoveGps { get; set; }
    public bool RemoveAllMetadata { get; set; }

    // New properties for linear stretch and adaptive sharpen
    public float LinearStretchBlackPointPercentage { get; set; }
    public float LinearStretchWhitePointPercentage { get; set; }
    public bool AdaptiveSharpen { get; set; } = false;

    public string? OutputFileName { get; set; }    // Assigned name in output folder
}
```
**Notes**:
- `QuickHash` could be a partial SHA256 of the first 1KB, file length, and last modified time.
- Linear stretch and adaptive sharpen parameters allow fine control over image adjustments.

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

    // New properties for linear stretch and adaptive sharpen
    public float LinearStretchBlackPointPercentage { get; set; }
    public float LinearStretchWhitePointPercentage { get; set; }
    public bool AdaptiveSharpen { get; set; }       // Copied from InputImage

    public bool Completed { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
```
**Notes**:
- These properties ensure that the optimization reflects the exact settings chosen at the time of queuing.

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
- In-memory caching only. May include disposal logic for large batches.

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
- Notifications are stored for UI binding and are now also part of the session file.

---

## 7. Image Processing Pipeline

Using **Magick.NET**, each queued task processes the image as follows:
1. **AutoOrient**
   - Reads EXIF orientation and corrects rotation/flip.
2. **TransformColorSpace**
   - Converts to sRGB or another standard color space.
   - Removes ICC profile once normalized.
3. **Remove ICC Profile**
   - Strips embedded profile post-conversion.
4. **Remove EXIF Thumbnail**
   - Eliminates any embedded EXIF preview.
5. **LinearStretch**
   - Adjusts brightness and contrast using specified black &amp; white point percentages.
   - Executed before saturation adjustment.
6. **Saturation Adjustment**
   - Applies the `SaturationUsed` value from the optimization task (100% = no change).
7. **Adaptive Sharpen**
   - Applies a light sharpening filter if enabled.
   - Does not affect thumbnail generation.
8. **Strip Metadata** (Conditional)
   - If `RemoveAllMetadata` is true, perform a full strip.
   - Else if `RemoveGps` is true, remove GPS tags specifically.

**Output**: Saved as a JPEG to the designated output file in the session’s directory.

---

## 8. Thumbnail Caching

- Thumbnails are cached in-memory.
- They are re-generated on each session load or when parameters (linear stretch, saturation) are adjusted.
- Note: Adaptive sharpen changes do not trigger thumbnail updates.
- No disk caching by default.

---

## 9. Restoring a Previous Session

When reopening an existing output directory or launching with a JSON file:
1. Load `session.json` to retrieve `InputImages`, `OptimizationTasks`, and notifications.
2. Verify existence of each file in `InputImages`:
   - Missing files are highlighted (e.g., in red).
3. Recompute `QuickHash` for present files:
   - Mismatches are flagged (e.g., in yellow) for user resolution.
4. Identify untracked output files:
   - Display a warning and offer to delete or keep them.
5. The UI enables continuation of adjustments or re-running tasks as needed.

---

## 10. Notification System

- A background process handles image processing.
- On errors or warnings (e.g., file locked, Magick exceptions), an `AppNotification` is created with a message and type (`Warning` or `Error`).
- Notifications are shown in a dedicated, non-blocking panel.
- Additionally, notifications are logged via Microsoft.Extensions.Logging with Serilog and stored in the session JSON.

---

## 11. Edge Cases & Special Modes

1. **Filenames with Timestamps**
   - Option to remove or ignore common timestamp prefixes.
2. **Burst/Sequential Suffixes**
   - Optionally cleaned up automatically.
3. **Output Filename Conflicts**
   - Overwrites are allowed due to unique `OutputFileName` generation.
4. **Large Batches**
   - Designed for ~100 images, with possibilities for virtualization or LRU caching if performance degrades.

---

## 12. Future Considerations

- Batch UI for group parameter adjustments.
- Undo/Redo functionality.
- Keyboard shortcuts for navigation.
- Enhanced concurrent processing (beyond 3 simultaneous tasks).
- Support for parameter presets (e.g., “Natural,” “Vivid,” “Black &amp; White”).
- CLI mode for headless batch processing.
- Expanded metadata editing capabilities.

---

## 13. Application Configuration, Logging, and Parallel Processing

- **Configuration**: The app reads settings from `appsettings.json` using Microsoft.Extensions.Configuration.
  - Configurable UI values include main window width, height, maximized state, window font family, base font size, and background color.
  - A toggle for activating Avalonia UI's dark theme is also included.
- **Parallel Processing**: Image optimization is executed using `Parallel.ForEach` with a maximum of 3 concurrent tasks. This degree of parallelism is configurable via `appsettings.json`.
- **Logging and JSON**:
  - Logging is managed by Microsoft.Extensions.Logging combined with Serilog.
  - All JSON operations use Microsoft’s built-in JSON implementations (e.g., System.Text.Json) instead of Newtonsoft.Json.

---

## 14. Acceptance Criteria

1. **Image Loading & Thumbnail Display**
   - Images load via drag-and-drop or file selection, and thumbnails display and update in real time for linear stretch and saturation changes.
2. **Parameter Adjustments**
   - Main window includes controls for linear stretch, saturation, adaptive sharpen, and GPS and Metadata Removal.
   - Linear stretch control (black and white point percentages) updates thumbnails immediately.
   - Adaptive sharpen toggle does not affect thumbnails.
3. **Optimization Trigger**
   - A global JPEG quality value is set before optimization; once any image is processed, this value becomes immutable.
   - The "Optimize" button triggers processing for all images, including those with unchanged parameters.
4. **Queue Management**
   - Rescheduled images are added to the front of the queue, ensuring immediate reprocessing for corrections.
   - Adjustments to an image in processing result in a new task being added; pending tasks are updated directly.
5. **Session Data Integrity**
   - `session.json` correctly contains all `InputImages`, `OptimizationTasks`, and notifications.
6. **Processing Pipeline**
   - The image processing pipeline applies linear stretch before saturation adjustment.
7. **Parallel Processing**
   - Configuration allows processing up to 3 images concurrently, as set in `appsettings.json`.
8. **Application Configuration & Logging**
   - Settings specified in `appsettings.json` (UI dimensions, dark theme, etc.) are applied.
   - Logging is implemented using Microsoft.Extensions.Logging with Serilog.
   - All JSON operations utilize Microsoft’s JSON libraries.
