using System;

namespace jpgOpt.App.Models;

public static class SessionDefaults
{
    // Image processing defaults
    public const bool EnableBlackPointAdjustment = true;
    public const float LinearStretchBlackPointPercentage = 0f;
    public const bool EnableWhitePointAdjustment = true;
    public const float LinearStretchWhitePointPercentage = 2f;
    public const float SaturationPercentage = 115f;
    public const bool AdaptiveSharpen = false;

    // Session-level defaults
    public const bool RemoveGps = true;
    public const bool RemoveAllMetadata = false;
    public const uint JpegQuality = 85;
    public const bool IsSessionLocked = false;
}