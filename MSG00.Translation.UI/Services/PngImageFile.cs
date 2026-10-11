using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using MSG00.Translation.Application.Features.Graphics;

namespace MSG00.Translation.UI.Services;

/// <summary>
/// Export and import of graphics as PNG (RGBA, transparent = color index 0), and preview bitmaps.
/// </summary>
public static class PngImageFile
{
    private static readonly FilePickerFileType PngFileType = new("PNG image") { Patterns = new List<string> { "*.png" } };

    public static WriteableBitmap ToBitmap(RgbaImage image)
    {
        var bitmap = new WriteableBitmap(new PixelSize(image.Width, image.Height), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Unpremul);
        using var buffer = bitmap.Lock();
        for (var y = 0; y < image.Height; y++)
        {
            Marshal.Copy(image.Pixels, y * image.Width * 4, buffer.Address + y * buffer.RowBytes, image.Width * 4);
        }

        return bitmap;
    }

    /// <returns>false when the user cancelled</returns>
    public static async Task<bool> ExportAsync(IStorageProvider storageProvider, string suggestedFileName, RgbaImage image)
    {
        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export image",
            DefaultExtension = "png",
            ShowOverwritePrompt = true,
            SuggestedFileName = suggestedFileName,
            FileTypeChoices = new List<FilePickerFileType> { PngFileType },
        });

        if (file == null)
        {
            return false;
        }

        await using var stream = await file.OpenWriteAsync();
        stream.SetLength(0);
        ToBitmap(image).Save(stream, PngBitmapEncoderOptions.Default);
        return true;
    }

    /// <returns>null when the user cancelled</returns>
    public static async Task<RgbaImage?> ImportAsync(IStorageProvider storageProvider)
    {
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Import image",
            FileTypeFilter = new List<FilePickerFileType> { PngFileType },
        });

        if (!files.Any())
        {
            return null;
        }

        await using var stream = await files[0].OpenReadAsync();
        using var loaded = new Bitmap(stream);

        var size = loaded.PixelSize;
        var pixels = new byte[size.Width * size.Height * 4];
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            loaded.CopyPixels(new PixelRect(size), handle.AddrOfPinnedObject(), pixels.Length, size.Width * 4);
        }
        finally
        {
            handle.Free();
        }

        // the decoder may return BGRA and/or premultiplied alpha; convert to straight RGBA
        var isBgra = loaded.Format == PixelFormat.Bgra8888;
        var isPremultiplied = loaded.AlphaFormat == AlphaFormat.Premul;
        for (var p = 0; p < pixels.Length; p += 4)
        {
            if (isBgra)
            {
                (pixels[p], pixels[p + 2]) = (pixels[p + 2], pixels[p]);
            }

            var alpha = pixels[p + 3];
            if (isPremultiplied && alpha is > 0 and < 255)
            {
                for (var c = 0; c < 3; c++)
                {
                    pixels[p + c] = (byte)Math.Min(255, pixels[p + c] * 255 / alpha);
                }
            }
        }

        return new RgbaImage(size.Width, size.Height, pixels);
    }
}