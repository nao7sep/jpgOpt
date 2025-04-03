using ImageMagick;

namespace jpgOpt.Core.Services
{
    public static class ImageProcessor
    {
        /// <summary>
        /// Process an image from a file path and write to a file.
        /// </summary>
        public static void ProcessImage (string inputFilePath,
            float linearStretchBlackPointPercentage, float linearStretchWhitePointPercentage,
            float saturationPercentage, bool adaptiveSharpen, bool removeGps, bool removeAllMetadata,
            string outputDirectoryPath, string outputFileName, MagickFormat format, uint? jpegQuality)
        {
            using var inputStream = new FileStream (inputFilePath, FileMode.Open, FileAccess.Read);
            var outputFilePath = Path.GetFullPath (Path.Combine (outputDirectoryPath, outputFileName));
            using var outputStream = new FileStream (outputFilePath, FileMode.Create, FileAccess.Write);

            ProcessImageInternal (inputStream, linearStretchBlackPointPercentage, linearStretchWhitePointPercentage,
                saturationPercentage, adaptiveSharpen, removeGps, removeAllMetadata, outputStream, format, jpegQuality);
        }

        /// <summary>
        /// Process an image from a stream and write to a file.
        /// </summary>
        public static void ProcessImage (Stream inputStream,
            float linearStretchBlackPointPercentage, float linearStretchWhitePointPercentage,
            float saturationPercentage, bool adaptiveSharpen, bool removeGps, bool removeAllMetadata,
            string outputDirectoryPath, string outputFileName, MagickFormat format, uint? jpegQuality)
        {
            var outputFilePath = Path.GetFullPath (Path.Combine (outputDirectoryPath, outputFileName));
            using var outputStream = new FileStream (outputFilePath, FileMode.Create, FileAccess.Write);

            ProcessImageInternal (inputStream, linearStretchBlackPointPercentage, linearStretchWhitePointPercentage,
                saturationPercentage, adaptiveSharpen, removeGps, removeAllMetadata, outputStream, format, jpegQuality);
        }

        /// <summary>
        /// Process an image from a file path and write to a stream.
        /// </summary>
        public static void ProcessImage (string inputFilePath,
            float linearStretchBlackPointPercentage, float linearStretchWhitePointPercentage,
            float saturationPercentage, bool adaptiveSharpen, bool removeGps, bool removeAllMetadata,
            Stream outputStream, MagickFormat format, uint? jpegQuality)
        {
            using var inputStream = new FileStream (inputFilePath, FileMode.Open, FileAccess.Read);

            ProcessImageInternal (inputStream, linearStretchBlackPointPercentage, linearStretchWhitePointPercentage,
                saturationPercentage, adaptiveSharpen, removeGps, removeAllMetadata, outputStream, format, jpegQuality);
        }

        /// <summary>
        /// Process an image from a stream and write to a stream.
        /// </summary>
        public static void ProcessImage (Stream inputStream,
            float linearStretchBlackPointPercentage, float linearStretchWhitePointPercentage,
            float saturationPercentage, bool adaptiveSharpen, bool removeGps, bool removeAllMetadata,
            Stream outputStream, MagickFormat format, uint? jpegQuality)
        {
            ProcessImageInternal (inputStream, linearStretchBlackPointPercentage, linearStretchWhitePointPercentage,
               saturationPercentage, adaptiveSharpen, removeGps, removeAllMetadata, outputStream, format, jpegQuality);
        }

        /// <summary>
        /// Internal method to process an image from an input stream and write to an output stream.
        /// </summary>
        private static void ProcessImageInternal (Stream inputStream,
            float linearStretchBlackPointPercentage, float linearStretchWhitePointPercentage,
            float saturationPercentage, bool adaptiveSharpen, bool removeGps, bool removeAllMetadata,
            Stream outputStream, MagickFormat format, uint? jpegQuality)
        {
            // https://github.com/nao7sep/_imgLab/blob/main/_imgLab/Utility.cs

            using var image = new MagickImage (inputStream);

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

            if (linearStretchBlackPointPercentage >= 0 || linearStretchWhitePointPercentage >= 0)
            {
                image.LinearStretch (
                    blackPoint: new Percentage (linearStretchBlackPointPercentage),
                    whitePoint: new Percentage (linearStretchWhitePointPercentage));
            }

            if (saturationPercentage != 100)
            {
                image.Modulate (
                    brightness: new Percentage (100),
                    saturation: new Percentage (saturationPercentage),
                    hue: new Percentage (100));
            }

            if (adaptiveSharpen)
                image.AdaptiveSharpen (radius: 0, sigma: 1);

            if (removeGps)
            {
                foreach (var key in image.AttributeNames)
                {
                    // Keys such as: GPSAltitude, GPSAltitudeRef, GPSDateStamp, GPSInfo, GPSLatitude, GPSLatitudeRef,
                    // GPSLongitude, GPSLongitudeRef, GPSProcessingMethod, GPSSpeed, GPSSpeedRef, GPSTimeStamp, GPSVersionID

                    if (key.StartsWith ("exif:GPS", StringComparison.OrdinalIgnoreCase))
                        image.RemoveAttribute (key);
                }
            }

            if (removeAllMetadata)
                image.Strip ();

            if (jpegQuality != null)
            {
                if (format != MagickFormat.Jpeg)
                    throw new ArgumentException ("jpegQuality can only be set when the format is jpeg.");

                image.Quality = jpegQuality.Value;
            }

            image.Write (outputStream, format);
        }
    }
}
