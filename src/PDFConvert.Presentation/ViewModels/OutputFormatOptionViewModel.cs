using PDFConvert.Domain.Enums;

namespace PDFConvert.Presentation.ViewModels;

public sealed class OutputFormatOptionViewModel : ObservableObject
{
    private bool _isSelected;

    public OutputFormatOptionViewModel(OutputFormat format, string displayName, bool isSelected = false)
    {
        Format = format;
        DisplayName = displayName;
        _isSelected = isSelected;
    }

    public OutputFormat Format { get; }

    public string DisplayName { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
