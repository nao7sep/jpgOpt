using ImageMagick;
using jpgOpt.Core.Models;

namespace jpgOpt.Core.Services
{
    public class ImageProcessor
    {
        public async Task ProcessImageAsync (OptimizationTask task, string outputDirectory, MagickFormat format, uint? jpegQuality)
        {
            await Task.Run (() =>
            {
                // https://github.com/nao7sep/_imgLab/blob/main/_imgLab/Utility.cs

                try
                {
                    Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

                    using var image = new MagickImage (task.InputImage.FilePath);

                    image.AutoOrient ();

                    if (image.HasProfile ("icc"))
                    {
                        image.TransformColorSpace (ColorProfile.SRGB);
                        image.RemoveProfile ("icc");
                    }

                    else if (image.ColorSpace != ColorSpace.sRGB)
                        image.TransformColorSpace (ColorProfile.AdobeRGB1998, ColorProfile.SRGB);

                    var exifProfile = image.GetExifProfile ();

                    if (exifProfile != null && exifProfile.ThumbnailOffset != 0 && exifProfile.ThumbnailLength != 0)
                    {
                        exifProfile.RemoveThumbnail ();
                        image.SetProfile (exifProfile);
                    }

                    if (task.LinearStretchBlackPointPercentage >= 0 || task.LinearStretchWhitePointPercentage >= 0)
                        image.LinearStretch (
                            blackPoint: new Percentage (task.LinearStretchBlackPointPercentage),
                            whitePoint: new Percentage (task.LinearStretchWhitePointPercentage));

                    if (task.SaturationPercentage != 100)
                        image.Modulate (
                            brightness: new Percentage (100),
                            saturation: new Percentage (task.SaturationPercentage),
                            hue: new Percentage (100));

                    if (task.AdaptiveSharpen)
                        image.AdaptiveSharpen (radius: 0, sigma: 1);

                    if (task.RemovedGps)
                    {
                        foreach (var key in image.AttributeNames)
                        {
                            // Keys such as: GPSAltitude, GPSAltitudeRef, GPSDateStamp, GPSInfo, GPSLatitude, GPSLatitudeRef,
                            // GPSLongitude, GPSLongitudeRef, GPSProcessingMethod, GPSSpeed, GPSSpeedRef, GPSTimeStamp, GPSVersionID

                            if (key.StartsWith ("exif:GPS", StringComparison.OrdinalIgnoreCase))
                                image.RemoveAttribute (key);
                        }
                    }

                    if (task.RemovedAllMetadata)
                        image.Strip ();

                    if (jpegQuality != null)
                    {
                        if (format != MagickFormat.Jpeg)
                            throw new ArgumentException ("jpegQuality can only be set when the format is jpeg.");

                        image.Quality = jpegQuality.Value;
                    }

                    var outputFilePath = Path.GetFullPath (Path.Combine (outputDirectory, task.OutputFileName));
                    image.Write (outputFilePath, format);
                }

                catch (Exception ex)
                {
                    task.ErrorMessage = ex.Message;
                }

                finally
                {
                    task.CompletedAtUtc = DateTime.UtcNow;
                }
            });
        }
    }
}
