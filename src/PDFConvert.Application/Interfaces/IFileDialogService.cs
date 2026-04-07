namespace PDFConvert.Application.Interfaces;

public interface IFileDialogService
{
    string? PickSourceFile(string? initialDirectory = null);
    string? PickOutputFolder(string? initialDirectory = null);
}
