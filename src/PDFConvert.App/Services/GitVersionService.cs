using System.Diagnostics;
using System.IO;
using PDFConvert.Application.Interfaces;
using PDFConvert.Domain.ValueObjects;

namespace PDFConvert.App.Services;

public sealed class GitVersionService : IGitVersionService
{
    public GitVersionInfo GetVersionInfo()
    {
        var repositoryRoot = TryFindRepositoryRoot();
        if (repositoryRoot is null)
        {
            return GitVersionInfo.Unavailable;
        }

        var commitCountText = RunGitCommand(repositoryRoot, "rev-list --count HEAD");
        var shortHash = RunGitCommand(repositoryRoot, "rev-parse --short=9 HEAD");

        if (!int.TryParse(commitCountText, out var commitCount))
        {
            return new GitVersionInfo(null, NormalizeShortHash(shortHash));
        }

        return new GitVersionInfo(commitCount, NormalizeShortHash(shortHash));
    }

    private static string NormalizeShortHash(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "---------";
        }

        var trimmed = value.Trim();
        return trimmed.Length >= 9 ? trimmed[..9] : trimmed.PadRight(9, '-');
    }

    private static string? TryFindRepositoryRoot()
    {
        var candidates = new[]
        {
            AppContext.BaseDirectory,
            Environment.CurrentDirectory,
        };

        foreach (var candidate in candidates)
        {
            var directory = new DirectoryInfo(candidate);
            while (directory is not null)
            {
                var gitDirectoryPath = Path.Combine(directory.FullName, ".git");
                if (Directory.Exists(gitDirectoryPath) || File.Exists(gitDirectoryPath))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        return null;
    }

    private static string? RunGitCommand(string workingDirectory, string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"-C \"{workingDirectory}\" {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(3000);

            return process.ExitCode == 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }
}
