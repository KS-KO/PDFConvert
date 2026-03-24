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

    public async Task<byte[]?> RenderPageAsPngAsync(int zeroBasedPageIndex, CancellationToken cancellationToken = default)
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
                DestinationWidth = (uint)Math.Max(1200, Math.Ceiling(page.Size.Width * 2)),
                DestinationHeight = (uint)Math.Max(1600, Math.Ceiling(page.Size.Height * 2)),
            };

            await page.RenderToStreamAsync(stream, options);
            cancellationToken.ThrowIfCancellationRequested();

            stream.Seek(0);
            using var managedStream = stream.AsStreamForRead();
            using var memoryStream = new MemoryStream();
            await managedStream.CopyToAsync(memoryStream, cancellationToken);
            return memoryStream.ToArray();
        }
        catch
        {
            return null;
        }
    }
}
