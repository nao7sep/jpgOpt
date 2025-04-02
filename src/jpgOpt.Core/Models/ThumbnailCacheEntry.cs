using System.Text.Json.Serialization;
using Avalonia.Media.Imaging;

namespace jpgOpt.Core.Models
{
    public class ThumbnailCacheEntry: IDisposable
    {
        public Guid Id { get; set; }

        public DateTime LoadedAtUtc { get; set; }

        public Guid InputImageId { get; set; }

        [JsonIgnore]
        public InputImage InputImage { get; set; } = null!;

        public Bitmap Thumbnail { get; set; } = null!;

        public void Dispose ()
        {
            Thumbnail?.Dispose ();
            GC.SuppressFinalize (this);
        }
    }
}
