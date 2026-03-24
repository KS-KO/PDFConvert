using PDFConvert.Infrastructure.Extraction;

namespace PDFConvert.Tests;

public sealed class PdfPageTextMergerTests
{
    [Fact]
    public void Merge_Does_Not_Add_Duplicate_Image_Text()
    {
        var structuredText = "Safety Overview" + Environment.NewLine + Environment.NewLine + "Laser class 4";
        var imageOnlyText = "Laser class 4";

        var merged = PdfPageTextMerger.Merge(structuredText, imageOnlyText);

        Assert.Equal(structuredText, merged);
    }

    [Fact]
    public void Merge_Appends_New_Image_Text_Only_Once()
    {
        var structuredText = "Safety Overview" + Environment.NewLine + Environment.NewLine + "Laser class 4";
        var imageOnlyText = "Emergency stop location";

        var merged = PdfPageTextMerger.Merge(structuredText, imageOnlyText);

        Assert.Contains("Safety Overview", merged);
        Assert.Contains("Laser class 4", merged);
        Assert.Contains("Emergency stop location", merged);
        Assert.Equal(1, CountOccurrences(merged, "Emergency stop location"));
    }

    private static int CountOccurrences(string text, string token)
    {
        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }
}
