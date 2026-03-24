namespace PDFConvert.Application.Interfaces;

public interface IFileDialogService
{
    string? PickPdfFile();
    string? PickOutputFolder(string? initialDirectory = null);
}
