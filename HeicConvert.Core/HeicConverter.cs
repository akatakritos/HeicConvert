using LibHeifSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Metadata.Profiles.Icc;
using SixLabors.ImageSharp.Metadata.Profiles.Xmp;
using SixLabors.ImageSharp.PixelFormats;

namespace HeicConvert.Core;

public static class HeicConverter
{
    public static void ConvertToJpg(string source, string destination)
    {
        var decodingOptions = new HeifDecodingOptions
        {
            ConvertHdrToEightBit = false,
            Strict = false,
            DecoderId = null
        };

        using var context = new HeifContext(source);
        using var primaryImage = context.GetPrimaryImageHandle();
        WriteOutputImage(primaryImage, decodingOptions, destination);
        File.SetCreationTime(destination, File.GetCreationTime(source));
        File.SetLastWriteTime(destination, File.GetLastWriteTime(source));
    }

    static void WriteOutputImage(HeifImageHandle imageHandle, HeifDecodingOptions decodingOptions, string outputPath)
    {
        Image? outputImage = null;
        try
        {
            HeifChroma chroma;
            bool hasAlpha = imageHandle.HasAlphaChannel;
            int bitDepth = imageHandle.BitDepth;

            if (bitDepth == 8 || decodingOptions.ConvertHdrToEightBit)
            {
                chroma = hasAlpha ? HeifChroma.InterleavedRgba32 : HeifChroma.InterleavedRgb24;
            }
            else
            {
                // Use the native byte order of the operating system.
                if (BitConverter.IsLittleEndian)
                {
                    chroma = hasAlpha ? HeifChroma.InterleavedRgba64LE : HeifChroma.InterleavedRgb48LE;
                }
                else
                {
                    chroma = hasAlpha ? HeifChroma.InterleavedRgba64BE : HeifChroma.InterleavedRgb48BE;
                }
            }

            using (var image = imageHandle.Decode(HeifColorspace.Rgb, chroma, decodingOptions))
            {
                var decodingWarnings = image.DecodingWarnings;

                foreach (var item in decodingWarnings)
                {
                    Console.WriteLine("Warning: " + item);
                }

                outputImage = CreateOutputImage(imageHandle, chroma, image);

                if (image.IccColorProfile != null)
                {
                    outputImage.Metadata.IccProfile = new IccProfile(image.IccColorProfile.GetIccProfileBytes());
                }
            }

            byte[] exif = imageHandle.GetExifMetadata();

            if (exif != null)
            {
                outputImage.Metadata.ExifProfile = new ExifProfile(exif);
                // The HEIF specification states that the EXIF orientation tag is only
                // informational and should not be used to rotate the image.
                // See https://github.com/strukturag/libheif/issues/227#issuecomment-642165942
                outputImage.Metadata.ExifProfile.RemoveValue(ExifTag.Orientation);
            }

            byte[] xmp = imageHandle.GetXmpMetadata();

            if (xmp != null)
            {
                outputImage.Metadata.XmpProfile = new XmpProfile(xmp);
            }

            outputImage.SaveAsJpeg(outputPath);
        }
        finally
        {
            outputImage?.Dispose();
        }
    }

    private static Image CreateOutputImage(HeifImageHandle imageHandle, HeifChroma chroma, HeifImage image)
    {
        Image outputImage;
        switch (chroma)
        {
            case HeifChroma.InterleavedRgb24:
                outputImage = CreateEightBitImageWithoutAlpha(image);
                break;
            case HeifChroma.InterleavedRgba32:
                outputImage = CreateEightBitImageWithAlpha(image, imageHandle.IsPremultipliedAlpha);
                break;
            case HeifChroma.InterleavedRgb48BE:
            case HeifChroma.InterleavedRgb48LE:
                outputImage = CreateSixteenBitImageWithoutAlpha(image);
                break;
            case HeifChroma.InterleavedRgba64BE:
            case HeifChroma.InterleavedRgba64LE:
                outputImage = CreateSixteenBitImageWithAlpha(image, imageHandle.IsPremultipliedAlpha,
                    imageHandle.BitDepth);
                break;
            default:
                throw new InvalidOperationException("Unsupported HeifChroma value.");
        }

        return outputImage;
    }

    static unsafe Image CreateEightBitImageWithAlpha(HeifImage heifImage, bool premultiplied)
    {
        var image = new Image<Rgba32>(heifImage.Width, heifImage.Height);

        var heifPlaneData = heifImage.GetPlane(HeifChannel.Interleaved);

        byte* srcScan0 = (byte*)heifPlaneData.Scan0;
        int srcStride = heifPlaneData.Stride;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                byte* src = srcScan0 + (y * srcStride);
                var dst = accessor.GetRowSpan(y);

                for (int x = 0; x < accessor.Width; x++)
                {
                    ref var pixel = ref dst[x];

                    if (premultiplied)
                    {
                        byte alpha = src[3];

                        switch (alpha)
                        {
                            case 0:
                                pixel.R = 0;
                                pixel.G = 0;
                                pixel.B = 0;
                                break;
                            case 255:
                                pixel.R = src[0];
                                pixel.G = src[1];
                                pixel.B = src[2];
                                break;
                            default:
                                pixel.R = (byte)Math.Min(MathF.Round(src[0] * 255f / alpha), 255);
                                pixel.G = (byte)Math.Min(MathF.Round(src[1] * 255f / alpha), 255);
                                pixel.B = (byte)Math.Min(MathF.Round(src[2] * 255f / alpha), 255);
                                break;
                        }
                    }
                    else
                    {
                        pixel.R = src[0];
                        pixel.G = src[1];
                        pixel.B = src[2];
                    }

                    pixel.A = src[3];

                    src += 4;
                }
            }
        });

        return image;
    }

    static unsafe Image CreateEightBitImageWithoutAlpha(HeifImage heifImage)
    {
        var image = new Image<Rgb24>(heifImage.Width, heifImage.Height);

        var heifPlaneData = heifImage.GetPlane(HeifChannel.Interleaved);

        byte* srcScan0 = (byte*)heifPlaneData.Scan0;
        int srcStride = heifPlaneData.Stride;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                byte* src = srcScan0 + (y * srcStride);
                var dst = accessor.GetRowSpan(y);

                for (int x = 0; x < accessor.Width; x++)
                {
                    ref var pixel = ref dst[x];

                    pixel.R = src[0];
                    pixel.G = src[1];
                    pixel.B = src[2];

                    src += 3;
                }
            }
        });

        return image;
    }


    static unsafe Image CreateSixteenBitImageWithAlpha(HeifImage heifImage, bool premultiplied, int bitDepth)
    {
        var image = new Image<Rgba64>(heifImage.Width, heifImage.Height);

        var heifPlaneData = heifImage.GetPlane(HeifChannel.Interleaved);

        byte* srcScan0 = (byte*)heifPlaneData.Scan0;
        int srcStride = heifPlaneData.Stride;

        int maxChannelValue = (1 << bitDepth) - 1;
        float maxChannelValueFloat = maxChannelValue;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                ushort* src = (ushort*)(srcScan0 + (y * srcStride));
                var dst = accessor.GetRowSpan(y);

                for (int x = 0; x < accessor.Width; x++)
                {
                    ref var pixel = ref dst[x];

                    if (premultiplied)
                    {
                        ushort alpha = src[3];

                        if (alpha == maxChannelValue)
                        {
                            pixel.R = src[0];
                            pixel.G = src[1];
                            pixel.B = src[2];
                        }
                        else
                        {
                            switch (alpha)
                            {
                                case 0:
                                    pixel.R = 0;
                                    pixel.G = 0;
                                    pixel.B = 0;
                                    break;
                                default:
                                    pixel.R = (ushort)Math.Min(MathF.Round(src[0] * maxChannelValueFloat / alpha),
                                        maxChannelValue);
                                    pixel.G = (ushort)Math.Min(MathF.Round(src[1] * maxChannelValueFloat / alpha),
                                        maxChannelValue);
                                    pixel.B = (ushort)Math.Min(MathF.Round(src[2] * maxChannelValueFloat / alpha),
                                        maxChannelValue);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        pixel.R = src[0];
                        pixel.G = src[1];
                        pixel.B = src[2];
                    }

                    pixel.A = src[3];

                    src += 4;
                }
            }
        });

        return image;
    }

    static unsafe Image CreateSixteenBitImageWithoutAlpha(HeifImage heifImage)
    {
        var image = new Image<Rgb48>(heifImage.Width, heifImage.Height);

        var heifPlaneData = heifImage.GetPlane(HeifChannel.Interleaved);

        byte* srcScan0 = (byte*)heifPlaneData.Scan0;
        int srcStride = heifPlaneData.Stride;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                ushort* src = (ushort*)(srcScan0 + (y * srcStride));
                var dst = accessor.GetRowSpan(y);

                for (int x = 0; x < accessor.Width; x++)
                {
                    ref var pixel = ref dst[x];

                    pixel.R = src[0];
                    pixel.G = src[1];
                    pixel.B = src[2];

                    src += 3;
                }
            }
        });

        return image;
    }
}