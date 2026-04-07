using System.IO;
using System.Windows;
using PDFConvert.Application.Interfaces;
using Forms = System.Windows.Forms;

namespace PDFConvert.App.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? PickSourceFile(string? initialDirectory = null)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "변환할 파일 선택",
            Filter = "PDF 및 이미지 파일 (*.pdf;*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff)|*.pdf;*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff|PDF Files (*.pdf)|*.pdf|Image Files (*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff)|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff|All Files (*.*)|*.*",
            DefaultExt = ".pdf",
            AddExtension = true,
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false,
            InitialDirectory = ResolveInitialDirectory(initialDirectory),
        };

        var owner = System.Windows.Application.Current?.MainWindow;
        var result = owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
        return result == true ? dialog.FileName : null;
    }

    public string? PickOutputFolder(string? initialDirectory = null)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            SelectedPath = ResolveInitialDirectory(initialDirectory) ?? Environment.CurrentDirectory,
            ShowNewFolderButton = true,
        };

        return dialog.ShowDialog() == Forms.DialogResult.OK ? dialog.SelectedPath : null;
    }

    private static string? ResolveInitialDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (Directory.Exists(path))
        {
            return path;
        }

        if (File.Exists(path))
        {
            return Path.GetDirectoryName(path);
        }

        return null;
    }
}
