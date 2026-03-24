using PDFConvert.Domain.Entities;
using PDFConvert.Infrastructure.Writers;

namespace PDFConvert.Tests;

public sealed class DocumentTextFormatterTests
{
    [Fact]
    public void Format_Uses_Short_First_Line_As_Title()
    {
        var page = new PdfTextPage
        {
            PageNumber = 1,
            Text = "Quarterly Business Review" + Environment.NewLine +
                   Environment.NewLine +
                   "Revenue increased year over year." + Environment.NewLine +
                   "Margins also improved.",
        };

        var formatted = DocumentTextFormatter.Format(page);

        Assert.Equal("Quarterly Business Review", formatted.Title);
        Assert.Single(formatted.Blocks);
        Assert.Equal("Revenue increased year over year. Margins also improved.", formatted.Blocks[0].Text);
        Assert.False(formatted.Blocks[0].IsListItem);
    }

    [Fact]
    public void Format_Falls_Back_To_Page_Title_For_Long_Opening_Line()
    {
        var page = new PdfTextPage
        {
            PageNumber = 3,
            Text = "This is a very long opening line that should not be treated as a title because it is too descriptive for a heading." +
                   Environment.NewLine +
                   "Follow-up line.",
        };

        var formatted = DocumentTextFormatter.Format(page);

        Assert.Equal("Page 3", formatted.Title);
        Assert.Single(formatted.Blocks);
    }

    [Fact]
    public void Format_Splits_Paragraphs_On_Blank_Lines()
    {
        var page = new PdfTextPage
        {
            PageNumber = 2,
            Text = "Summary" + Environment.NewLine +
                   Environment.NewLine +
                   "First paragraph line one." + Environment.NewLine +
                   "First paragraph line two." + Environment.NewLine +
                   Environment.NewLine +
                   "Second paragraph.",
        };

        var formatted = DocumentTextFormatter.Format(page);

        Assert.Equal(2, formatted.Blocks.Count);
        Assert.Equal("First paragraph line one. First paragraph line two.", formatted.Blocks[0].Text);
        Assert.Equal("Second paragraph.", formatted.Blocks[1].Text);
    }

    [Fact]
    public void Format_Keeps_Bullet_And_Numbered_Lines_As_List_Items()
    {
        var page = new PdfTextPage
        {
            PageNumber = 4,
            Text = "Action Items" + Environment.NewLine +
                   Environment.NewLine +
                   "- Prepare launch plan" + Environment.NewLine +
                   "1. Review timeline" + Environment.NewLine +
                   Environment.NewLine +
                   "Closing paragraph.",
        };

        var formatted = DocumentTextFormatter.Format(page);

        Assert.Equal(3, formatted.Blocks.Count);
        Assert.True(formatted.Blocks[0].IsListItem);
        Assert.True(formatted.Blocks[1].IsListItem);
        Assert.False(formatted.Blocks[2].IsListItem);
    }
}
