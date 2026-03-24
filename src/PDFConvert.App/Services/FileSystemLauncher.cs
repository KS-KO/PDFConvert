using System.Diagnostics;
using System.IO;
using PDFConvert.Application.Interfaces;

namespace PDFConvert.App.Services;

public sealed class FileSystemLauncher : IFileSystemLauncher
{
    public void OpenFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = folderPath,
            UseShellExecute = true,
        });
    }
}
