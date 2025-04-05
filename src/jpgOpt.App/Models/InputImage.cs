using System;

namespace jpgOpt.App.Models;

public class InputImage
{
    public Guid Id { get; set; }

    public string FilePath { get; set; } = null!;

    public DateTime LastModifiedAtUtc { get; set; }

    public long FileLength { get; set; }

    public string XxHashDigest { get; set; } = null!;

    public int Width { get; set; }

    public int Height { get; set; }

    public float LinearStretchBlackPointPercentage { get; set; }

    public float LinearStretchWhitePointPercentage { get; set; }

    public float SaturationPercentage { get; set; }

    public bool AdaptiveSharpen { get; set; }

    public bool RemoveGps { get; set; }

    public bool RemoveAllMetadata { get; set; }

    public string OutputFileName { get; set; } = null!;
}