using System.Text.Json.Serialization;

namespace jpgOpt.Core.Models;

public class Session
{
    public Guid Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime LastModifiedAtUtc { get; set; }

    [JsonIgnore]
    public string OutputDirectoryPath { get; set; } = null!;

    [JsonIgnore]
    public DirectoryInfo OutputDirectory { get; set; } = null!;

    [JsonIgnore]
    public FileInfo OutputFile { get; set; } = null!;

    public uint JpegQuality { get; set; }

    public bool IsJpegQualityLocked { get; set; }

    public List<InputImage> InputImages { get; set; } = [];

    public List<OptimizationTask> OptimizationTasks { get; set; } = [];

    public List<AppNotification> Notifications { get; set; } = [];
}