using System.Text.RegularExpressions;

namespace PDFConvert.Infrastructure.Extraction;

internal static partial class OcrTextPostProcessor
{
    private static readonly Regex NotebookLmRegex = new(@"\bNotebookLM\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SeparatorRegex = new(@"^[-=_.|\s]+$", RegexOptions.Compiled);
    private static readonly Regex MultiWhitespaceRegex = new(@"\s{2,}", RegexOptions.Compiled);

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
        var cleaned = NotebookLmRegex.Replace(line, string.Empty).Trim();
        cleaned = SeparatorRegex.Replace(cleaned, string.Empty).Trim();
        return cleaned;
    }

    private static string CollapseWhitespace(string line)
    {
        return MultiWhitespaceRegex.Replace(line, " ").Trim();
    }
}
