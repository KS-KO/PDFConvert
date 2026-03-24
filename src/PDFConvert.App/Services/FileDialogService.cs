using PDFConvert.Application.Interfaces;
using Forms = System.Windows.Forms;

namespace PDFConvert.App.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? PickPdfFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "PDF 파일 선택",
            Filter = "PDF Files (*.pdf)|*.pdf",
            CheckFileExists = true,
            Multiselect = false,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickOutputFolder(string? initialDirectory = null)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            InitialDirectory = string.IsNullOrWhiteSpace(initialDirectory) ? Environment.CurrentDirectory : initialDirectory,
            ShowNewFolderButton = true,
        };

        return dialog.ShowDialog() == Forms.DialogResult.OK ? dialog.SelectedPath : null;
    }
}
