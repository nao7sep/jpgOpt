using System;
using System.Text.Json.Serialization;

namespace jpgOpt.App.Models;

public class OptimizationTask
{
    public Guid Id { get; set; }

    public DateTime QueuedAtUtc { get; set; }

    public Guid InputImageId { get; set; }

    [JsonIgnore]
    public InputImage InputImage { get; set; } = null!;

    public float LinearStretchBlackPointPercentage { get; set; }

    public float LinearStretchWhitePointPercentage { get; set; }

    public float SaturationPercentage { get; set; }

    public bool AdaptiveSharpen { get; set; }

    public bool RemovedGps { get; set; }

    public bool RemovedAllMetadata { get; set; }

    public string OutputFileName { get; set; } = null!;

    public DateTime? CompletedAtUtc { get; set; }

    public string? ErrorMessage { get; set; }
}