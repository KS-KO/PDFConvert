namespace PDFConvert.Infrastructure.Extraction;

internal static class PdfPageTextMerger
{
    public static string Merge(string structuredText, string? imageOnlyText)
    {
        var st = (structuredText ?? string.Empty).Trim();
        var it = (imageOnlyText ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(st)) return it;
        if (string.IsNullOrWhiteSpace(it)) return st;

        var mergedLines = SplitToLines(st).ToList();
        var knownKeys = mergedLines.Select(Canonicalize).Where(k => !string.IsNullOrWhiteSpace(k)).ToHashSet();

        // More granular line-by-line merging
        foreach (var imgLine in SplitToLines(it))
        {
            var key = Canonicalize(imgLine);
            if (string.IsNullOrWhiteSpace(key)) continue;

            // Only skip if the line is significantly present already
            if (knownKeys.Any(k => k.Contains(key, StringComparison.Ordinal) || key.Contains(k, StringComparison.Ordinal)))
            {
                continue;
            }

            mergedLines.Add(imgLine);
            knownKeys.Add(key);
        }

        return string.Join(Environment.NewLine, mergedLines);
    }

    private static IEnumerable<string> SplitToLines(string text)
    {
        return text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l));
    }

    private static string Canonicalize(string text)
    {
        return new string(text.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    }
}
