# Image Saturation Optimizer Specification v0.1

## Table of Contents
1. [Introduction & Goals](#introduction--goals)
2. [High-Level Overview](#high-level-overview)
3. [App Workflow](#app-workflow)
4. [UI & UX Requirements](#ui--ux-requirements)
5. [Session & Output Management](#session--output-management)
6. [Models & Data Structures](#models--data-structures)
   - [Session](#model-session)
   - [InputImage](#model-inputimage)
   - [OptimizationTask](#model-optimizationtask)
   - [AppConfiguration](#model-appconfiguration)
   - [AppNotification](#model-appnotification--notificationtype)
7. [Image Processing Pipeline](#image-processing-pipeline)
8. [Thumbnail Caching](#thumbnail-caching)
9. [Restoring a Previous Session](#restoring-a-previous-session)
10. [Notification System](#notification-system)
11. [Edge Cases & Special Modes](#edge-cases--special-modes)
12. [Future Considerations](#future-considerations)
13. [Application Configuration](#application-configuration)

---

## 1. Introduction & Goals

This **Avalonia UI** application enables users to:
1. **Load multiple images** via file dialog or drag-and-drop
2. **Preview images** as thumbnails with adjustable parameters
3. **Adjust Image Parameters**:
   - **Linear Stretch**: Black point and white point percentage sliders
   - **Saturation Slider**: Real-time saturation adjustment
   - **Adaptive Sharpen**: Toggle for sharpening (default off)
   - **GPS and Metadata Removal**: Options to remove GPS data or all metadata
4. **Queue optimization tasks** with a dedicated "Optimize" button
5. **Output to a timestamped directory** containing optimized JPEGs and a session file
6. **Restore** previous sessions with change detection

*Key emphasis*: Optimization is triggered manually to allow parameter review before processing. When "Optimize" is pressed, all images are processed to apply uniform JPEG quality and metadata modifications.

---

## 2. High-Level Overview

1. **Main Window**
   - Displays thumbnails of loaded images
   - Shows controls in this order: linear stretch, saturation, adaptive sharpen, GPS and metadata removal
   - Updates thumbnails in real-time for linear stretch and saturation changes

2. **Output Folder**
   - Named `jpgOpt-<timestamp>` (e.g., `jpgOpt-20250401-144510`)
   - Contains optimized images and session.json

3. **Data Models**
   - **`Session`**: Overall session information
   - **`InputImage`**: Original image properties and editing parameters
   - **`OptimizationTask`**: Queued jobs with parameters at queue time
   - **`AppConfiguration`**: Application configuration settings
   - **`AppNotification`**: Warnings and errors

4. **Processing Behavior**
   - JPEG quality is set globally and becomes immutable once processing begins
   - Rescheduled images are prioritized in the queue
   - Parameter adjustments for queued images update pending tasks or create new ones if already processing

---

## 3. App Workflow

1. **Image Loading**
   - Add via drag-and-drop or file dialog
   - Duplicates are ignored
   - Thumbnails generate immediately

2. **Parameter Adjustment**
   - Modify linear stretch, saturation, adaptive sharpen, and metadata settings
   - Changes update thumbnails in real-time (except adaptive sharpen)

3. **Optimization Trigger**
   - Set global JPEG quality before pressing "Optimize"
   - All images are processed to apply quality and metadata settings
   - JPEG quality becomes locked after first optimization

4. **Task Management**
   - Changed parameters for queued images:
     - If processing: new task added to front of queue
     - If pending: parameters updated in place

5. **Session Closure**
   - Confirmation required if pending tasks exist
   - Graceful termination on OS session end

---

## 4. UI & UX Requirements

1. **Thumbnail List**
   - Scrolling panel with virtualization
   - Single selection for parameter adjustments

2. **Linear Stretch Control**
   - Black Point and White Point Percentage sliders
   - Immediate thumbnail preview updates

3. **Saturation Slider**
   - Range: 0-200% (100% = no change)
   - Enabled only when one image is selected

4. **Adaptive Sharpen Toggle**
   - Simple on/off control (default off)
   - No thumbnail preview updates

5. **Metadata Checkboxes**
   - Remove GPS or all metadata options

6. **JPEG Quality Control**
   - Single value set before optimization
   - Immutable after processing begins

7. **Optimize Button**
   - Explicitly queues images for processing

8. **Delete Button**
   - Cancels pending tasks and removes images

9. **Notification Panel**
   - Non-blocking display for errors and warnings

10. **Control Order**
    - Linear Stretch → Saturation → Adaptive Sharpen → GPS/Metadata Removal

11. **User Guidance**
    - Clear explanation for manual optimization trigger

---

## 5. Session & Output Management

1. **Session Handling**
   - One session per application run
   - Data stored in `session.json` within output directory
   - Includes InputImages, OptimizationTasks, and notifications

2. **Output Structure**
   - Directory: `jpgOpt-<Local timestamp in yyyyMMdd-HHmmss>`
   - Contains processed images and session data

3. **Session File (`session.json`)**
   - Lists for InputImages, OptimizationTasks, and notifications
   - Unique SessionId (GUID)
   - Note: InputImage.FilePath may contain absolute file paths as source images are typically in their final storage locations

---

## 6. Models & Data Structures

### Model: `Session`
```csharp
public class Session
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string OutputDirectory { get; set; }
    public int JpegQuality { get; set; }
    public List<InputImage> InputImages { get; set; } = new List<InputImage>();
    public List<OptimizationTask> OptimizationTasks { get; set; } = new List<OptimizationTask>();
    public List<AppNotification> Notifications { get; set; } = new List<AppNotification>();
}
```
**Notes**:
- Represents the overall session information
- Contains references to all images, tasks, and notifications
- Stores the JPEG quality setting that becomes immutable after processing begins

### Model: `InputImage`
```csharp
public class InputImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FilePath { get; set; }
    public DateTime FileLastModifiedUtc { get; set; }
    public long FileLength { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    // Optimization parameters
    public float? LinearStretchBlackPointPercentage { get; set; }
    public float? LinearStretchWhitePointPercentage { get; set; }
    public float SaturationAdjustment { get; set; } = 100;
    public bool AdaptiveSharpen { get; set; } = false;

    // Metadata parameters
    public bool RemoveAllMetadata { get; set; }
    public bool RemoveGps { get; set; }
}
```
**Notes**:
- File-related properties are grouped together before image-related properties
- Linear stretch parameters are nullable - if null, they weren't applied
- Saturation is not nullable as 100 means no change
- Parameters control image adjustments

### Model: `OptimizationTask`
```csharp
public class OptimizationTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string InputPath { get; set; }
    public DateTime QueuedAtUtc { get; set; }

    // Optimization parameters (copied from InputImage at queue time)
    public float? LinearStretchBlackPointPercentage { get; set; }
    public float? LinearStretchWhitePointPercentage { get; set; }
    public float SaturationAdjustment { get; set; }
    public bool AdaptiveSharpen { get; set; }

    // Metadata parameters (copied from InputImage at queue time)
    public bool RemovedAllMetadata { get; set; }
    public bool RemovedGps { get; set; }

    public DateTime? CompletedAtUtc { get; set; }
}
```
**Notes**:
- Captures settings at queue time
- Output file path derived from InputPath and session output directory
- InputPath must be identical to FilePath of the corresponding InputImage for joining tables
- Linear stretch parameters are nullable - if null, they weren't applied
- Task is considered completed when CompletedAtUtc is not null

### Model: `AppConfiguration`
```csharp
public class AppConfiguration
{
    public int DefaultJpegQuality { get; set; } = 85;
    public int MaxConcurrentTasks { get; set; } = 3;
    public int ThumbnailSize { get; set; } = 200;
    public bool DarkMode { get; set; } = false;
    public string DefaultOutputDirectory { get; set; }
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;
}
```
**Notes**:
- Stores application-wide configuration settings
- Loaded from appsettings.json using Microsoft.Extensions.Configuration
- Can be modified through a settings UI

### Model: `AppNotification` & `NotificationType`
```csharp
public class AppNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public string Message { get; set; } = string.Empty;
}

public enum NotificationType
{
    Info,
    Warning,
    Error
}
```
**Notes**:
- Stored for UI binding and in session file

---

## 7. Image Processing Pipeline

Using **Magick.NET**, each queued task processes images as follows:
1. **AutoOrient** - Corrects rotation/flip based on EXIF
2. **TransformColorSpace** - Converts to sRGB
3. **Remove ICC Profile** - Strips embedded profile
4. **Remove EXIF Thumbnail** - Eliminates embedded preview
5. **LinearStretch** - Adjusts brightness/contrast using black & white point percentages
6. **Saturation Adjustment** - Applies saturation value (100% = no change)
7. **Adaptive Sharpen** - Applies light sharpening if enabled
8. **Strip Metadata** (Conditional) - Removes GPS or all metadata as specified

**Output**: Saved as JPEG to the session's output directory

---

## 8. Thumbnail Caching

- In-memory cache only (no disk caching)
- Regenerated on session load or parameter adjustments
- Adaptive sharpen changes do not trigger updates

**Note**: The implementation details of thumbnail caching will be determined during development. The specification does not define a specific model class for this functionality at this time.

---

## 9. Restoring a Previous Session

When reopening an existing session:
1. Load `session.json` for InputImages, OptimizationTasks, and notifications
2. Verify file existence (missing files will be ignored in the thumbnail list)
3. Display notifications for missing files (properly colored to gain user attention)
4. Enable continuation of adjustments or re-running tasks

---

## 10. Notification System

- Background processing with error/warning notifications
- Notifications displayed in non-blocking panel
- Logged via Microsoft.Extensions.Logging with Serilog
- Stored in session JSON

---

## 11. Edge Cases & Special Modes

1. **Output Filename Conflicts**
   - Prevents adding input files with identical filenames
   - When identically named files are attempted to be added, they will be ignored
   - User will receive notifications about ignored files
   - Ensures unique output without renaming

2. **Large Batches**
   - Designed for ~100 images
   - Virtualization for larger batches

---

## 12. Future Considerations

- Batch UI for group parameter adjustments
- Undo/Redo functionality
- Keyboard shortcuts
- Enhanced concurrent processing
- Parameter presets ("Natural," "Vivid," "B&W")
- CLI mode for headless processing
- Expanded metadata editing

---

## 13. Application Configuration

- Settings from `appsettings.json` using Microsoft.Extensions.Configuration
- Configurable UI: window dimensions, font, colors, theme
- Parallel processing with max 3 concurrent tasks (configurable)
- Logging via Microsoft.Extensions.Logging with Serilog
- JSON operations using Microsoft's libraries

---

*End of Image Saturation Optimizer v0.1 specification.*