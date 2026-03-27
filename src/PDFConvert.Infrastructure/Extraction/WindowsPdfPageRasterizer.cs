using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PDFConvert.Infrastructure.Extraction;

internal sealed class WindowsPdfPageRasterizer
{
    private readonly PdfDocument _document;

    private WindowsPdfPageRasterizer(PdfDocument document)
    {
        _document = document;
    }

    public static async Task<WindowsPdfPageRasterizer?> CreateAsync(string pdfPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(pdfPath);
            cancellationToken.ThrowIfCancellationRequested();

            var document = await PdfDocument.LoadFromFileAsync(storageFile);
            return new WindowsPdfPageRasterizer(document);
        }
        catch
        {
            return null;
        }
    }

    public async Task<RenderedPdfPage?> RenderPageAsPngAsync(int zeroBasedPageIndex, CancellationToken cancellationToken = default)
    {
        if (zeroBasedPageIndex < 0 || zeroBasedPageIndex >= _document.PageCount)
        {
            return null;
        }

        try
        {
            using var page = _document.GetPage((uint)zeroBasedPageIndex);
            using var stream = new InMemoryRandomAccessStream();

            var options = new PdfPageRenderOptions
            {
                DestinationWidth = (uint)Math.Max(2000, Math.Ceiling(page.Size.Width * 4)),
                DestinationHeight = (uint)Math.Max(2800, Math.Ceiling(page.Size.Height * 4)),
            };

            await page.RenderToStreamAsync(stream, options);
            cancellationToken.ThrowIfCancellationRequested();

            stream.Seek(0);
            using var managedStream = stream.AsStreamForRead();
            using var memoryStream = new MemoryStream();
            await managedStream.CopyToAsync(memoryStream, cancellationToken);

            return new RenderedPdfPage
            {
                PngBytes = memoryStream.ToArray(),
                PixelWidth = (int)options.DestinationWidth,
                PixelHeight = (int)options.DestinationHeight,
            };
        }
        catch
        {
            return null;
        }
    }
}

internal sealed class RenderedPdfPage
{
    public byte[] PngBytes { get; init; } = [];

    public int PixelWidth { get; init; }

    public int PixelHeight { get; init; }
}
