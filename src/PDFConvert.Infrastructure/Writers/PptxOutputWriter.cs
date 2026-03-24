using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using PDFConvert.Application.Interfaces;
using PDFConvert.Domain.Entities;
using PDFConvert.Domain.Enums;

namespace PDFConvert.Infrastructure.Writers;

public sealed class PptxOutputWriter : IOutputWriter
{
    public OutputFormat Format => OutputFormat.Pptx;

    public Task<string> WriteAsync(PdfDocumentContent document, string outputDirectory, CancellationToken cancellationToken = default)
    {
        var outputPath = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(document.SourceFilePath)}.pptx");

        using var presentationDocument = PresentationDocument.Create(outputPath, PresentationDocumentType.Presentation);
        var presentationPart = presentationDocument.AddPresentationPart();
        presentationPart.Presentation = new Presentation();

        var slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>();
        GenerateSlideMasterPart(slideMasterPart);

        var slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>();
        GenerateSlideLayoutPart(slideLayoutPart);
        slideMasterPart.SlideMaster.AppendChild(new SlideLayoutIdList(
            new SlideLayoutId { Id = 1U, RelationshipId = slideMasterPart.GetIdOfPart(slideLayoutPart) }));
        slideMasterPart.SlideMaster.Save();

        var themePart = slideMasterPart.AddNewPart<ThemePart>();
        GenerateThemePart(themePart);

        presentationPart.Presentation.SlideMasterIdList = new SlideMasterIdList(
            new SlideMasterId { Id = 2147483648U, RelationshipId = presentationPart.GetIdOfPart(slideMasterPart) });

        var slideIdList = new SlideIdList();
        uint slideId = 256U;

        foreach (var page in document.Pages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var slidePart = presentationPart.AddNewPart<SlidePart>();
            GenerateSlidePart(slidePart, slideLayoutPart, page);
            slideIdList.Append(new SlideId { Id = slideId++, RelationshipId = presentationPart.GetIdOfPart(slidePart) });
        }

        presentationPart.Presentation.Append(slideIdList);
        presentationPart.Presentation.SlideSize = new SlideSize { Cx = 12192000, Cy = 6858000 };
        presentationPart.Presentation.Save();

        return Task.FromResult(outputPath);
    }

    private static void GenerateSlidePart(SlidePart slidePart, SlideLayoutPart slideLayoutPart, PdfTextPage page)
    {
        var formattedPage = DocumentTextFormatter.Format(page);
        var slide = new Slide(new CommonSlideData(new ShapeTree()));
        var shapeTree = slide.CommonSlideData!.ShapeTree!;

        shapeTree.Append(
            new NonVisualGroupShapeProperties(
                new NonVisualDrawingProperties { Id = 1U, Name = string.Empty },
                new NonVisualGroupShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new GroupShapeProperties(new A.TransformGroup()));

        shapeTree.Append(CreateTextShape(2U, [new DocumentTextFormatter.FormattedBlock(formattedPage.Title, false)], 457200, 228600, 11277600, 685800));
        shapeTree.Append(CreateTextShape(3U, formattedPage.Blocks, 457200, 1143000, 11277600, 5029200));

        slidePart.Slide = slide;
        slidePart.AddPart(slideLayoutPart);
        slide.Save();
    }

    private static Shape CreateTextShape(uint id, IReadOnlyList<DocumentTextFormatter.FormattedBlock> blocks, long x, long y, long cx, long cy)
    {
        var textBody = new TextBody(
            new A.BodyProperties(),
            new A.ListStyle());

        textBody.Append(BuildParagraphs(blocks).ToArray());

        return new Shape(
            new NonVisualShapeProperties(
                new NonVisualDrawingProperties { Id = id, Name = $"TextBox {id}" },
                new NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
                new ApplicationNonVisualDrawingProperties()),
            new ShapeProperties(
                new A.Transform2D(new A.Offset { X = x, Y = y }, new A.Extents { Cx = cx, Cy = cy })),
            textBody);
    }

    private static IEnumerable<A.Paragraph> BuildParagraphs(IReadOnlyList<DocumentTextFormatter.FormattedBlock> blocks)
    {
        foreach (var block in blocks)
        {
            var paragraph = new A.Paragraph(
                new A.Run(
                    new A.RunProperties { Language = "ko-KR", FontSize = 1800 },
                    new A.Text(block.Text)));

            if (block.IsListItem)
            {
                paragraph.ParagraphProperties = new A.ParagraphProperties { Level = 0 };
            }

            yield return paragraph;
        }
    }

    private static void GenerateSlideMasterPart(SlideMasterPart slideMasterPart)
    {
        slideMasterPart.SlideMaster = new SlideMaster(
            new CommonSlideData(new ShapeTree(
                new NonVisualGroupShapeProperties(
                    new NonVisualDrawingProperties { Id = 1U, Name = string.Empty },
                    new NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new A.TransformGroup()))),
            new ColorMap
            {
                Background1 = A.ColorSchemeIndexValues.Light1,
                Text1 = A.ColorSchemeIndexValues.Dark1,
                Background2 = A.ColorSchemeIndexValues.Light2,
                Text2 = A.ColorSchemeIndexValues.Dark2,
                Accent1 = A.ColorSchemeIndexValues.Accent1,
                Accent2 = A.ColorSchemeIndexValues.Accent2,
                Accent3 = A.ColorSchemeIndexValues.Accent3,
                Accent4 = A.ColorSchemeIndexValues.Accent4,
                Accent5 = A.ColorSchemeIndexValues.Accent5,
                Accent6 = A.ColorSchemeIndexValues.Accent6,
                Hyperlink = A.ColorSchemeIndexValues.Hyperlink,
                FollowedHyperlink = A.ColorSchemeIndexValues.FollowedHyperlink,
            });
    }

    private static void GenerateSlideLayoutPart(SlideLayoutPart slideLayoutPart)
    {
        slideLayoutPart.SlideLayout = new SlideLayout(
            new CommonSlideData(new ShapeTree(
                new NonVisualGroupShapeProperties(
                    new NonVisualDrawingProperties { Id = 1U, Name = string.Empty },
                    new NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new A.TransformGroup()))),
            new ColorMapOverride(new A.MasterColorMapping()));
        slideLayoutPart.SlideLayout.Save();
    }

    private static void GenerateThemePart(ThemePart themePart)
    {
        themePart.Theme = new A.Theme(
            new A.ThemeElements(
                new A.ColorScheme(
                    new A.Dark1Color(new A.SystemColor { Val = A.SystemColorValues.WindowText, LastColor = "000000" }),
                    new A.Light1Color(new A.SystemColor { Val = A.SystemColorValues.Window, LastColor = "FFFFFF" }),
                    new A.Dark2Color(new A.RgbColorModelHex { Val = "1F2937" }),
                    new A.Light2Color(new A.RgbColorModelHex { Val = "F8FAFC" }),
                    new A.Accent1Color(new A.RgbColorModelHex { Val = "2563EB" }),
                    new A.Accent2Color(new A.RgbColorModelHex { Val = "0F766E" }),
                    new A.Accent3Color(new A.RgbColorModelHex { Val = "D97706" }),
                    new A.Accent4Color(new A.RgbColorModelHex { Val = "9333EA" }),
                    new A.Accent5Color(new A.RgbColorModelHex { Val = "DC2626" }),
                    new A.Accent6Color(new A.RgbColorModelHex { Val = "4F46E5" }),
                    new A.Hyperlink(new A.RgbColorModelHex { Val = "2563EB" }),
                    new A.FollowedHyperlinkColor(new A.RgbColorModelHex { Val = "7C3AED" }))
                { Name = "Office" },
                new A.FontScheme(
                    new A.MajorFont(
                        new A.LatinFont { Typeface = "Arial" },
                        new A.EastAsianFont { Typeface = "Malgun Gothic" },
                        new A.ComplexScriptFont { Typeface = "Arial" }),
                    new A.MinorFont(
                        new A.LatinFont { Typeface = "Arial" },
                        new A.EastAsianFont { Typeface = "Malgun Gothic" },
                        new A.ComplexScriptFont { Typeface = "Arial" }))
                { Name = "Office" },
                new A.FormatScheme(
                    new A.FillStyleList(new A.SolidFill(new A.SchemeColor { Val = A.SchemeColorValues.PhColor })),
                    new A.LineStyleList(new A.Outline()),
                    new A.EffectStyleList(new A.EffectStyle()),
                    new A.BackgroundFillStyleList(new A.SolidFill(new A.SchemeColor { Val = A.SchemeColorValues.PhColor })))
                { Name = "Office" }))
        { Name = "PDFConvertTheme" };

        themePart.Theme.Save();
    }
}
