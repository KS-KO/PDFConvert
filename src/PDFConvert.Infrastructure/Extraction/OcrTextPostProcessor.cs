using System.Text.RegularExpressions;

namespace PDFConvert.Infrastructure.Extraction;

internal static partial class OcrTextPostProcessor
{
    public static string Clean(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalized.Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(RemoveWatermark)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(CollapseWhitespace)
            .ToList();

        return string.Join(Environment.NewLine, lines);
    }

    private static string RemoveWatermark(string line)
    {
        var cleaned = NotebookLmPattern().Replace(line, string.Empty).Trim();
        cleaned = SeparatorPattern().Replace(cleaned, string.Empty).Trim();
        return cleaned;
    }

    private static string CollapseWhitespace(string line)
    {
        return MultiWhitespacePattern().Replace(line, " ").Trim();
    }

    [GeneratedRegex(@"\bNotebookLM\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NotebookLmPattern();

    [GeneratedRegex(@"^[-=_.|\s]+$", RegexOptions.Compiled)]
    private static partial Regex SeparatorPattern();

    [GeneratedRegex(@"\s{2,}", RegexOptions.Compiled)]
    private static partial Regex MultiWhitespacePattern();
}
