using PDFConvert.Domain.Enums;

namespace PDFConvert.Presentation.ViewModels;

public sealed class OcrEngineOptionViewModel
{
    public OcrEngineOptionViewModel(OcrEngineKind engineKind, string displayName)
    {
        EngineKind = engineKind;
        DisplayName = displayName;
    }

    public OcrEngineKind EngineKind { get; }

    public string DisplayName { get; }
}
