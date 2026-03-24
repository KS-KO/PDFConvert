namespace PDFConvert.Infrastructure.Extraction;

internal static class PdfPageTextMerger
{
    public static string Merge(string structuredText, string? imageOnlyText)
    {
        var normalizedStructuredText = Normalize(structuredText);
        var normalizedImageOnlyText = Normalize(imageOnlyText ?? string.Empty);

        if (string.IsNullOrWhiteSpace(normalizedStructuredText))
        {
            return normalizedImageOnlyText;
        }

        if (string.IsNullOrWhiteSpace(normalizedImageOnlyText))
        {
            return normalizedStructuredText;
        }

        var mergedBlocks = SplitBlocks(normalizedStructuredText).ToList();
        var knownKeys = mergedBlocks
            .Select(Canonicalize)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToList();

        foreach (var imageBlock in SplitBlocks(normalizedImageOnlyText))
        {
            var imageKey = Canonicalize(imageBlock);
            if (string.IsNullOrWhiteSpace(imageKey))
            {
                continue;
            }

            var isDuplicate = knownKeys.Any(knownKey =>
                knownKey.Contains(imageKey, StringComparison.Ordinal) ||
                imageKey.Contains(knownKey, StringComparison.Ordinal));

            if (isDuplicate)
            {
                continue;
            }

            mergedBlocks.Add(imageBlock);
            knownKeys.Add(imageKey);
        }

        return string.Join(Environment.NewLine + Environment.NewLine, mergedBlocks);
    }

    private static IReadOnlyList<string> SplitBlocks(string text)
    {
        return text
            .Split([Environment.NewLine + Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)
            .Select(block => block.Trim())
            .Where(block => !string.IsNullOrWhiteSpace(block))
            .ToArray();
    }

    private static string Canonicalize(string text)
    {
        return new string(text
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace("\n", Environment.NewLine)
            .Trim();
    }
}
