namespace PDFConvert.Application.Interfaces;

public interface IFileDialogService
{
    string? PickPdfFile(string? initialDirectory = null);
    string? PickOutputFolder(string? initialDirectory = null);
}
