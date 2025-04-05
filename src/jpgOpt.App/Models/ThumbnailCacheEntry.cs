using System;
using Avalonia.Media.Imaging;

namespace jpgOpt.App.Models;

public class ThumbnailCacheEntry : IDisposable
{
    public Guid Id { get; set; }

    public DateTime LoadedAtUtc { get; set; }

    public Guid InputImageId { get; set; }

    public InputImage InputImage { get; set; } = null!;

    public Bitmap Thumbnail { get; set; } = null!;

    public void Dispose()
    {
        Thumbnail?.Dispose();
        GC.SuppressFinalize(this);
    }
}