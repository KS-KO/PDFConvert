using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using PDFConvert.Domain.Entities;

namespace PDFConvert.Infrastructure.Writers;

internal sealed class PageImageSampler : IDisposable
{
    private readonly byte[] _originalImageBytes;
    private readonly byte[] _pixels;

    private PageImageSampler(byte[] originalImageBytes, byte[] pixels, int width, int height)
    {
        _originalImageBytes = originalImageBytes;
        _pixels = pixels;
        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }

    public static PageImageSampler? Create(byte[] imageBytes)
    {
        if (imageBytes.Length == 0)
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(imageBytes, writable: false);
            using var randomAccessStream = stream.AsRandomAccessStream();
            var decoder = BitmapDecoder.CreateAsync(randomAccessStream).AsTask().GetAwaiter().GetResult();
            var bitmap = decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied).AsTask().GetAwaiter().GetResult();

            var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
            bitmap.CopyToBuffer(pixels.AsBuffer());

            return new PageImageSampler(imageBytes.ToArray(), pixels, bitmap.PixelWidth, bitmap.PixelHeight);
        }
        catch
        {
            return null;
        }
    }

    public string SampleBackgroundHex(PdfTextOverlay overlay)
    {
        var color = SampleBackgroundColor(overlay);
        return $"{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
    }

    public string SampleFontHex(PdfTextOverlay overlay, string? backgroundHex = null)
    {
        var bgColor = backgroundHex != null
            ? new RgbColor(Convert.ToByte(backgroundHex.Substring(0, 2), 16),
                           Convert.ToByte(backgroundHex.Substring(2, 2), 16),
                           Convert.ToByte(backgroundHex.Substring(4, 2), 16))
            : SampleBackgroundColor(overlay);

        var fontColor = SampleFontColor(overlay, bgColor);
        return $"{fontColor.Red:X2}{fontColor.Green:X2}{fontColor.Blue:X2}";
    }

    public void EraseTextArea(PdfTextOverlay overlay)
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        var left = ClampPixel((int)Math.Floor(overlay.LeftRatio * Width), Width);
        var top = ClampPixel((int)Math.Floor(overlay.TopRatio * Height), Height);
        var right = ClampPixel((int)Math.Ceiling((overlay.LeftRatio + overlay.WidthRatio) * Width), Width);
        var bottom = ClampPixel((int)Math.Ceiling((overlay.TopRatio + overlay.HeightRatio) * Height), Height);

        if (right <= left || bottom <= top)
        {
            return;
        }

        var horizontalPadding = Math.Max(2, (right - left) / 8);
        var verticalPadding = Math.Max(2, (bottom - top) / 5);
        var fill = SampleBackgroundColor(overlay);

        var fillLeft = Math.Max(0, left - horizontalPadding);
        var fillRight = Math.Min(Width, right + horizontalPadding);
        var fillTop = Math.Max(0, top - verticalPadding);
        var fillBottom = Math.Min(Height, bottom + verticalPadding);

        for (var y = fillTop; y < fillBottom; y++)
        {
            for (var x = fillLeft; x < fillRight; x++)
            {
                var index = ((y * Width) + x) * 4;
                if (index + 3 >= _pixels.Length)
                {
                    continue;
                }

                _pixels[index] = fill.Blue;
                _pixels[index + 1] = fill.Green;
                _pixels[index + 2] = fill.Red;
                _pixels[index + 3] = 255;
            }
        }
    }

    public byte[] ExportPng()
    {
        try
        {
            var bitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, Width, Height, BitmapAlphaMode.Premultiplied);
            bitmap.CopyFromBuffer(_pixels.AsBuffer());

            using var stream = new InMemoryRandomAccessStream();
            var encoder = BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream).AsTask().GetAwaiter().GetResult();
            encoder.SetSoftwareBitmap(bitmap);
            encoder.FlushAsync().AsTask().GetAwaiter().GetResult();

            stream.Seek(0);
            using var managedStream = stream.AsStreamForRead();
            using var memory = new MemoryStream();
            managedStream.CopyTo(memory);
            return memory.ToArray();
        }
        catch
        {
            return _originalImageBytes.ToArray();
        }
    }

    public void Dispose()
    {
    }

    private void SampleRing(
        int left,
        int right,
        int top,
        int bottom,
        int horizontalPadding,
        int verticalPadding,
        ref long red,
        ref long green,
        ref long blue,
        ref long count)
    {
        var outerLeft = Math.Max(0, left - horizontalPadding);
        var outerRight = Math.Min(Width, right + horizontalPadding);
        var outerTop = Math.Max(0, top - verticalPadding);
        var outerBottom = Math.Min(Height, bottom + verticalPadding);

        SampleArea(outerLeft, outerRight, outerTop, top, ref red, ref green, ref blue, ref count);
        SampleArea(outerLeft, outerRight, bottom, outerBottom, ref red, ref green, ref blue, ref count);
        SampleArea(outerLeft, left, top, bottom, ref red, ref green, ref blue, ref count);
        SampleArea(right, outerRight, top, bottom, ref red, ref green, ref blue, ref count);
    }

    private void SampleFill(
        int left,
        int right,
        int top,
        int bottom,
        ref long red,
        ref long green,
        ref long blue,
        ref long count)
    {
        var centerLeft = left + ((right - left) / 4);
        var centerRight = right - ((right - left) / 4);
        var centerTop = top + ((bottom - top) / 4);
        var centerBottom = bottom - ((bottom - top) / 4);

        SampleArea(centerLeft, centerRight, centerTop, centerBottom, ref red, ref green, ref blue, ref count);
    }

    private void SampleArea(
        int left,
        int right,
        int top,
        int bottom,
        ref long red,
        ref long green,
        ref long blue,
        ref long count)
    {
        if (right <= left || bottom <= top)
        {
            return;
        }

        var stepX = Math.Max(1, (right - left) / 12);
        var stepY = Math.Max(1, (bottom - top) / 6);

        for (var y = top; y < bottom; y += stepY)
        {
            for (var x = left; x < right; x += stepX)
            {
                var index = ((y * Width) + x) * 4;
                if (index + 3 >= _pixels.Length)
                {
                    continue;
                }

                var alpha = _pixels[index + 3];
                if (alpha == 0)
                {
                    continue;
                }

                blue += _pixels[index];
                green += _pixels[index + 1];
                red += _pixels[index + 2];
                count++;
            }
        }
    }

    private static int ClampPixel(int value, int maxExclusive)
    {
        return Math.Clamp(value, 0, maxExclusive);
    }

    private RgbColor SampleBackgroundColor(PdfTextOverlay overlay)
    {
        if (Width <= 0 || Height <= 0)
        {
            return new RgbColor(255, 255, 255);
        }

        var left = ClampPixel((int)Math.Floor(overlay.LeftRatio * Width), Width);
        var top = ClampPixel((int)Math.Floor(overlay.TopRatio * Height), Height);
        var right = ClampPixel((int)Math.Ceiling((overlay.LeftRatio + overlay.WidthRatio) * Width), Width);
        var bottom = ClampPixel((int)Math.Ceiling((overlay.TopRatio + overlay.HeightRatio) * Height), Height);

        if (right <= left || bottom <= top)
        {
            return new RgbColor(255, 255, 255);
        }

        var horizontalPadding = Math.Max(2, (right - left) / 8);
        var verticalPadding = Math.Max(2, (bottom - top) / 5);

        long red = 0;
        long green = 0;
        long blue = 0;
        long count = 0;

        SampleRing(left, right, top, bottom, horizontalPadding, verticalPadding, ref red, ref green, ref blue, ref count);

        if (count == 0)
        {
            SampleFill(left, right, top, bottom, ref red, ref green, ref blue, ref count);
        }

        if (count == 0)
        {
            return new RgbColor(255, 255, 255);
        }

        return new RgbColor((byte)(red / count), (byte)(green / count), (byte)(blue / count));
    }

    private RgbColor SampleFontColor(PdfTextOverlay overlay, RgbColor bgColor)
    {
        if (Width <= 0 || Height <= 0)
        {
            return new RgbColor(0, 0, 0);
        }

        var left = ClampPixel((int)Math.Floor(overlay.LeftRatio * Width), Width);
        var top = ClampPixel((int)Math.Floor(overlay.TopRatio * Height), Height);
        var right = ClampPixel((int)Math.Ceiling((overlay.LeftRatio + overlay.WidthRatio) * Width), Width);
        var bottom = ClampPixel((int)Math.Ceiling((overlay.TopRatio + overlay.HeightRatio) * Height), Height);

        if (right <= left || bottom <= top)
        {
            return new RgbColor(0, 0, 0);
        }

        var candidatePixels = new List<RgbColor>();
        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                var index = ((y * Width) + x) * 4;
                if (index + 3 >= _pixels.Length) continue;

                var r = _pixels[index + 2];
                var g = _pixels[index + 1];
                var b = _pixels[index];
                
                var dist = Math.Sqrt(Math.Pow(r - bgColor.Red, 2) + Math.Pow(g - bgColor.Green, 2) + Math.Pow(b - bgColor.Blue, 2));
                if (dist > 35) // Different enough from background
                {
                    candidatePixels.Add(new RgbColor(r, g, b));
                }
            }
        }

        if (candidatePixels.Count == 0)
        {
            return new RgbColor(0, 0, 0);
        }

        // Return the average of candidates
        long rSum = 0, gSum = 0, bSum = 0;
        foreach (var p in candidatePixels)
        {
            rSum += p.Red;
            gSum += p.Green;
            bSum += p.Blue;
        }

        return new RgbColor((byte)(rSum / candidatePixels.Count), (byte)(gSum / candidatePixels.Count), (byte)(bSum / candidatePixels.Count));
    }

    private readonly record struct RgbColor(byte Red, byte Green, byte Blue);
}
