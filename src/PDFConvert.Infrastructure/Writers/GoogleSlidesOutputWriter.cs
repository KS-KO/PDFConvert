using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Slides.v1;
using Google.Apis.Slides.v1.Data;
using PDFConvert.Application.Interfaces;
using PDFConvert.Domain.Entities;
using PDFConvert.Domain.Enums;

namespace PDFConvert.Infrastructure.Writers;

public sealed class GoogleSlidesOutputWriter : IOutputWriter
{
    private readonly IOcrSettingsStore _settingsStore;

    private static readonly string[] Scopes =
    [
        SlidesService.Scope.Presentations,
        DriveService.Scope.DriveFile,
        DriveService.Scope.DriveReadonly
    ];

    public GoogleSlidesOutputWriter(IOcrSettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    public OutputFormat Format => OutputFormat.GoogleSlides;

    public async Task<string> WriteAsync(PdfDocumentContent document, string outputDirectory, CancellationToken cancellationToken = default)
    {
        var clientId = _settingsStore.GetGoogleClientId();
        var clientSecret = _settingsStore.GetGoogleClientSecret();

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException("Google Slides 연동을 위해 설정에서 Client ID와 Client Secret을 입력해 주세요.");
        }

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
            Scopes,
            "user",
            cancellationToken);

        using var slidesService = new SlidesService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "PDFConvert"
        });

        using var driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "PDFConvert"
        });

        // Resolve Page Size from first page
        var firstPage = document.Pages.FirstOrDefault();
        var (globalWidthPoints, globalHeightPoints) = firstPage != null 
            ? (firstPage.PointsWidth, firstPage.PointsHeight) 
            : (720.0, 405.0);

        // 1. Create Presentation with correct aspect ratio
        var presentation = await slidesService.Presentations.Create(new Presentation
        {
            Title = Path.GetFileNameWithoutExtension(document.SourceFilePath),
            PageSize = new Size
            {
                Width = new Dimension { Magnitude = globalWidthPoints, Unit = "PT" },
                Height = new Dimension { Magnitude = globalHeightPoints, Unit = "PT" }
            }
        }).ExecuteAsync(cancellationToken);

        var presentationId = presentation.PresentationId;
        var requests = new List<Request>();
        var tempImageIds = new List<string>();

        try
        {
            foreach (var page in document.Pages)
            {
                var slideId = Guid.NewGuid().ToString("N");
                requests.Add(new Request
                {
                    CreateSlide = new CreateSlideRequest
                    {
                        ObjectId = slideId,
                        SlideLayoutReference = new LayoutReference { PredefinedLayout = "BLANK" }
                    }
                });

                // Scale factors if page size differs from global
                var scaleX = page.PointsWidth > 0 ? globalWidthPoints / page.PointsWidth : 1.0;
                var scaleY = page.PointsHeight > 0 ? globalHeightPoints / page.PointsHeight : 1.0;

                // 2. Add Images
                foreach (var img in page.ImageOverlays)
                {
                    var fileId = await UploadTempImageAsync(driveService, img.ImageBytes, cancellationToken);
                    if (fileId != null)
                    {
                        tempImageIds.Add(fileId);
                        var webUrl = $"https://docs.google.com/uc?export=download&id={fileId}";

                        requests.Add(new Request
                        {
                            CreateImage = new CreateImageRequest
                            {
                                ObjectId = Guid.NewGuid().ToString("N"),
                                Url = webUrl,
                                ElementProperties = new PageElementProperties
                                {
                                    PageObjectId = slideId,
                                    Size = new Size
                                    {
                                        Width = new Dimension { Magnitude = img.WidthRatio * globalWidthPoints, Unit = "PT" },
                                        Height = new Dimension { Magnitude = img.HeightRatio * globalHeightPoints, Unit = "PT" }
                                    },
                                    Transform = new AffineTransform
                                    {
                                        ScaleX = 1.0,
                                        ScaleY = 1.0,
                                        TranslateX = img.LeftRatio * globalWidthPoints,
                                        TranslateY = img.TopRatio * globalHeightPoints,
                                        Unit = "PT"
                                    }
                                }
                            }
                        });
                    }
                }

                // 3. Add Text Overlays
                foreach (var overlay in page.TextOverlays.Where(o => !string.IsNullOrWhiteSpace(o.Text)))
                {
                    var textId = Guid.NewGuid().ToString("N");
                    
                    requests.Add(new Request
                    {
                        CreateShape = new CreateShapeRequest
                        {
                            ObjectId = textId,
                            ShapeType = "TEXT_BOX",
                            ElementProperties = new PageElementProperties
                            {
                                PageObjectId = slideId,
                                Size = new Size
                                {
                                    Width = new Dimension { Magnitude = overlay.WidthRatio * globalWidthPoints, Unit = "PT" },
                                    Height = new Dimension { Magnitude = overlay.HeightRatio * globalHeightPoints, Unit = "PT" }
                                },
                                Transform = new AffineTransform
                                {
                                    ScaleX = 1.0,
                                    ScaleY = 1.0,
                                    TranslateX = overlay.LeftRatio * globalWidthPoints,
                                    TranslateY = overlay.TopRatio * globalHeightPoints,
                                    Unit = "PT"
                                }
                            }
                        }
                    });

                    requests.Add(new Request
                    {
                        InsertText = new InsertTextRequest
                        {
                            ObjectId = textId,
                            Text = overlay.Text
                        }
                    });

                    // Better font style
                    requests.Add(new Request
                    {
                        UpdateTextStyle = new UpdateTextStyleRequest
                        {
                            ObjectId = textId,
                            Fields = "foregroundColor,fontSize,fontFamily",
                            Style = new TextStyle
                            {
                                FontSize = new Dimension { Magnitude = CalculateFontSize(overlay, globalHeightPoints), Unit = "PT" },
                                FontFamily = "Arial",
                                ForegroundColor = new OptionalColor
                                {
                                    OpaqueColor = new OpaqueColor
                                    {
                                        RgbColor = !string.IsNullOrWhiteSpace(overlay.FontColorHex) 
                                            ? ParseHex(overlay.FontColorHex) 
                                            : new RgbColor { Red = 0, Green = 0, Blue = 0 }
                                    }
                                }
                            }
                        }
                    });
                }
            }

            // Remove the default first slide
            if (presentation.Slides?.Count > 0)
            {
                requests.Add(new Request { DeleteObject = new DeleteObjectRequest { ObjectId = presentation.Slides[0].ObjectId } });
            }

            if (requests.Count > 0)
            {
                await slidesService.Presentations.BatchUpdate(new BatchUpdatePresentationRequest { Requests = requests }, presentationId).ExecuteAsync(cancellationToken);
            }
        }
        finally
        {
            // Note: In real world, you might want to wait a bit or let images load before deleting.
            // For now, we leave them or implement a cleanup strategy.
            // Cleanup(driveService, tempImageIds);
        }

        return $"https://docs.google.com/presentation/d/{presentationId}/edit";
    }

    private static double CalculateFontSize(PdfTextOverlay overlay, double pageHeightPoints)
    {
        var heightPoints = overlay.HeightRatio * pageHeightPoints;
        return Math.Clamp(heightPoints * 0.8, 6, 48); // Adaptive font size
    }

    private static async Task<string?> UploadTempImageAsync(DriveService service, byte[] bytes, CancellationToken cancellationToken)
    {
        try
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = $"pdf_img_{Guid.NewGuid():N}.png",
                MimeType = "image/png"
            };

            using var stream = new MemoryStream(bytes);
            var request = service.Files.Create(fileMetadata, stream, "image/png");
            request.Fields = "id";
            var file = await request.UploadAsync(cancellationToken);

            if (file.Status == Google.Apis.Upload.UploadStatus.Completed)
            {
                var fileId = request.ResponseBody.Id;
                
                // Allow anyone with link to view (required for Slides API to fetch)
                await service.Permissions.Create(new Permission
                {
                    Role = "reader",
                    Type = "anyone"
                }, fileId).ExecuteAsync(cancellationToken);

                return fileId;
            }
        }
        catch
        {
            // Ignore upload errors for robustness
        }
        return null;
    }

    private static RgbColor ParseHex(string hex)
    {
        if (hex.StartsWith('#')) hex = hex[1..];
        if (hex.Length != 6) return new RgbColor { Red = 0, Green = 0, Blue = 0 };

        try
        {
            return new RgbColor
            {
                Red = Convert.ToInt32(hex[..2], 16) / 255.0f,
                Green = Convert.ToInt32(hex[2..4], 16) / 255.0f,
                Blue = Convert.ToInt32(hex[4..6], 16) / 255.0f
            };
        }
        catch { return new RgbColor { Red = 0, Green = 0, Blue = 0 }; }
    }
}
