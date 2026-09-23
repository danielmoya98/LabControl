using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace LabControl.Client.Kiosk.Services;

public static class ScreenCaptureService
{
    public static byte[]? CapturarPantallaMiniatura(int targetWidth = 360, int targetHeight = 202, long calidad = 55L)
    {
        try
        {
            int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            if (screenWidth <= 0 || screenHeight <= 0)
            {
                screenWidth = 1920;
                screenHeight = 1080;
            }

            using var bmpScreenshot = new Bitmap(screenWidth, screenHeight, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmpScreenshot))
            {
                g.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(screenWidth, screenHeight), CopyPixelOperation.SourceCopy);
            }

            // Redimensionar a miniatura liviana
            using var thumbnail = new Bitmap(targetWidth, targetHeight);
            using (var gThumb = Graphics.FromImage(thumbnail))
            {
                gThumb.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                gThumb.DrawImage(bmpScreenshot, 0, 0, targetWidth, targetHeight);
            }

            // Comprimir a JPEG
            using var ms = new MemoryStream();
            var encoder = GetEncoder(ImageFormat.Jpeg);
            if (encoder != null)
            {
                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, calidad);
                thumbnail.Save(ms, encoder, encoderParameters);
            }
            else
            {
                thumbnail.Save(ms, ImageFormat.Jpeg);
            }

            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static ImageCodecInfo? GetEncoder(ImageFormat format)
    {
        var codecs = ImageCodecInfo.GetImageEncoders();
        foreach (var codec in codecs)
        {
            if (codec.FormatID == format.Guid)
            {
                return codec;
            }
        }
        return null;
    }
}
