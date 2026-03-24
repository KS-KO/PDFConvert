using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using PIC = DocumentFormat.OpenXml.Presentation.Picture;
using PDFConvert.Application.Interfaces;
using PDFConvert.Domain.Entities;
using PDFConvert.Domain.Enums;

namespace PDFConvert.Infrastructure.Writers;

public sealed class PptxOutputWriter : IOutputWriter
{
    private const long DefaultSlideWidth = 12192000L;
    private const long DefaultSlideHeight = 6858000L;

    public OutputFormat Format => OutputFormat.Pptx;

    public Task<string> WriteAsync(PdfDocumentContent document, string outputDirectory, CancellationToken cancellationToken = default)
    {
        var outputPath = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(document.SourceFilePath)}.pptx");
        var (slideWidth, slideHeight) = ResolveSlideSize(document);

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
            GenerateSlidePart(slidePart, slideLayoutPart, page, slideWidth, slideHeight);
            slideIdList.Append(new SlideId { Id = slideId++, RelationshipId = presentationPart.GetIdOfPart(slidePart) });
        }

        presentationPart.Presentation.Append(slideIdList);
        presentationPart.Presentation.SlideSize = new SlideSize { Cx = (int)slideWidth, Cy = (int)slideHeight };
        presentationPart.Presentation.Save();

        return Task.FromResult(outputPath);
    }

    private static (long Width, long Height) ResolveSlideSize(PdfDocumentContent document)
    {
        var firstVisualPage = document.Pages.FirstOrDefault(page =>
            page.RenderedPageImagePng is { Length: > 0 } &&
            page.RenderedPagePixelWidth > 0 &&
            page.RenderedPagePixelHeight > 0);

        if (firstVisualPage is null)
        {
            return (DefaultSlideWidth, DefaultSlideHeight);
        }

        var aspectRatio = firstVisualPage.RenderedPagePixelHeight / (double)firstVisualPage.RenderedPagePixelWidth;
        var calculatedHeight = (long)Math.Round(DefaultSlideWidth * aspectRatio);
        return (DefaultSlideWidth, Math.Max(1, calculatedHeight));
    }

    private static void GenerateSlidePart(
        SlidePart slidePart,
        SlideLayoutPart slideLayoutPart,
        PdfTextPage page,
        long slideWidth,
        long slideHeight)
    {
        var slide = new Slide(new CommonSlideData(new ShapeTree()));
        var shapeTree = slide.CommonSlideData!.ShapeTree!;

        shapeTree.Append(
            new NonVisualGroupShapeProperties(
                new NonVisualDrawingProperties { Id = 1U, Name = string.Empty },
                new NonVisualGroupShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new GroupShapeProperties(new A.TransformGroup()));

        if (page.RenderedPageImagePng is { Length: > 0 })
        {
            var imageBytes = page.RenderedPageImagePng;
            using var imageSampler = PageImageSampler.Create(page.RenderedPageImagePng);
            if (imageSampler is not null)
            {
                foreach (var overlay in page.TextOverlays.Where(overlay => !string.IsNullOrWhiteSpace(overlay.Text)))
                {
                    imageSampler.EraseTextArea(overlay);
                }

                imageBytes = imageSampler.ExportPng();
            }

            shapeTree.Append(CreateBackgroundImage(slidePart, imageBytes, slideWidth, slideHeight));

            uint nextShapeId = 20U;
            foreach (var overlay in page.TextOverlays.Where(overlay => !string.IsNullOrWhiteSpace(overlay.Text)))
            {
                shapeTree.Append(CreateOverlayTextShape(nextShapeId++, overlay, slideWidth, slideHeight));
            }
        }
        else
        {
            var formattedPage = DocumentTextFormatter.Format(page);
            shapeTree.Append(CreateTextShape(2U, [new DocumentTextFormatter.FormattedBlock(formattedPage.Title, false)], 457200, 228600, 11277600, 685800));
            shapeTree.Append(CreateTextShape(3U, formattedPage.Blocks, 457200, 1143000, 11277600, 5029200));
        }

        slidePart.Slide = slide;
        slidePart.AddPart(slideLayoutPart);
        slide.Save();
    }

    private static PIC CreateBackgroundImage(SlidePart slidePart, byte[] imageBytes, long slideWidth, long slideHeight)
    {
        var imagePart = slidePart.AddImagePart(ImagePartType.Png);
        using var stream = new MemoryStream(imageBytes, writable: false);
        imagePart.FeedData(stream);

        var relationshipId = slidePart.GetIdOfPart(imagePart);

        return new PIC(
            new NonVisualPictureProperties(
                new NonVisualDrawingProperties { Id = 2U, Name = "Page Image" },
                new NonVisualPictureDrawingProperties(new A.PictureLocks { NoChangeAspect = false }),
                new ApplicationNonVisualDrawingProperties()),
            new BlipFill(
                new A.Blip { Embed = relationshipId, CompressionState = A.BlipCompressionValues.Print },
                new A.Stretch(new A.FillRectangle())),
            new ShapeProperties(
                new A.Transform2D(
                    new A.Offset { X = 0L, Y = 0L },
                    new A.Extents { Cx = slideWidth, Cy = slideHeight }),
                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }));
    }

    private static Shape CreateOverlayTextShape(uint id, PdfTextOverlay overlay, long slideWidth, long slideHeight)
    {
        var left = ToEmu(overlay.LeftRatio, slideWidth);
        var top = ToEmu(overlay.TopRatio, slideHeight);
        var width = Math.Max(1L, ToEmu(overlay.WidthRatio, slideWidth));
        var height = Math.Max(1L, ToEmu(overlay.HeightRatio, slideHeight));
        var fontSize = ResolveFontSize(overlay.Text, width, height);

        var paragraph = new A.Paragraph(
            new A.ParagraphProperties { Alignment = A.TextAlignmentTypeValues.Left },
            new A.Run(
                new A.RunProperties { Language = "ko-KR", FontSize = fontSize, Dirty = false },
                new A.Text(overlay.Text)),
            new A.EndParagraphRunProperties { Language = "ko-KR", FontSize = fontSize });

        return new Shape(
            new NonVisualShapeProperties(
                new NonVisualDrawingProperties { Id = id, Name = $"OverlayText {id}" },
                new NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
                new ApplicationNonVisualDrawingProperties()),
            new ShapeProperties(
                new A.Transform2D(
                    new A.Offset { X = left, Y = top },
                    new A.Extents { Cx = width, Cy = height }),
                new A.NoFill(),
                new A.Outline(new A.NoFill())),
            new TextBody(
                new A.BodyProperties
                {
                    Wrap = A.TextWrappingValues.None,
                    LeftInset = 0,
                    TopInset = 0,
                    RightInset = 0,
                    BottomInset = 0,
                    Anchor = A.TextAnchoringTypeValues.Top,
                    VerticalOverflow = A.TextVerticalOverflowValues.Overflow,
                },
                new A.ListStyle(),
                paragraph));
    }

    private static long ToEmu(double ratio, long size)
    {
        return (long)Math.Round(Math.Clamp(ratio, 0, 1) * size);
    }

    private static int ResolveFontSize(string text, long overlayWidthEmu, long overlayHeightEmu)
    {
        var heightPoints = overlayHeightEmu / 12700d;
        var widthPoints = overlayWidthEmu / 12700d;
        var textUnits = Math.Max(EstimateTextUnits(text), 1d);
        var widthLimitedPoints = widthPoints / textUnits;
        var resolvedPoints = Math.Min(heightPoints * 0.96d, widthLimitedPoints * 1.04d);

        return (int)Math.Clamp(Math.Round(resolvedPoints * 100), 800, 3200);
    }

    private static double EstimateTextUnits(string text)
    {
        var units = 0d;

        foreach (var character in text)
        {
            units += character switch
            {
                ' ' => 0.32d,
                >= '0' and <= '9' => 0.56d,
                >= 'A' and <= 'Z' => 0.68d,
                >= 'a' and <= 'z' => 0.56d,
                <= '\u007f' when char.IsPunctuation(character) => 0.34d,
                _ when IsEastAsian(character) => 1.0d,
                _ => 0.78d,
            };
        }

        return units;
    }

    private static bool IsEastAsian(char character)
    {
        return character is >= '\u1100' and <= '\u11ff'
            or >= '\u2e80' and <= '\u9fff'
            or >= '\uac00' and <= '\ud7af'
            or >= '\uf900' and <= '\ufaff';
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
