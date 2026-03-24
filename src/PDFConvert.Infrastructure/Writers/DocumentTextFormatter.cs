using PDFConvert.Domain.Entities;

namespace PDFConvert.Infrastructure.Writers;

internal static class DocumentTextFormatter
{
    public static FormattedPage Format(PdfTextPage page)
    {
        var lines = page.Text
            .Split(Environment.NewLine, StringSplitOptions.None)
            .Select(line => line.Trim())
            .ToList();

        RemoveLeadingDuplicates(lines);

        var nonEmptyLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
        var firstNonEmpty = nonEmptyLines.FirstOrDefault();
        var titleCandidate = SelectTitleCandidate(lines, nonEmptyLines);
        var title = IsTitleCandidate(titleCandidate) && titleCandidate is not null
            ? titleCandidate
            : $"Page {page.PageNumber}";

        if (titleCandidate is not null)
        {
            var index = lines.FindIndex(line => line == titleCandidate);
            if (index >= 0)
            {
                lines.RemoveAt(index);
            }
        }

        var blocks = BuildBlocks(lines);

        if (blocks.Count == 0 && !string.IsNullOrWhiteSpace(firstNonEmpty))
        {
            blocks.Add(new FormattedBlock(firstNonEmpty, false));
        }

        return new FormattedPage(title, blocks);
    }

    private static void RemoveLeadingDuplicates(List<string> lines)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < lines.Count;)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                if (seen.Count > 0)
                {
                    break;
                }

                i++;
                continue;
            }

            if (!seen.Add(line))
            {
                lines.RemoveAt(i);
                continue;
            }

            if (seen.Count >= 3)
            {
                break;
            }

            i++;
        }
    }

    private static bool IsTitleCandidate(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        if (line.Length > 80)
        {
            return false;
        }

        var wordCount = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return wordCount <= 12;
    }

    private static string? SelectTitleCandidate(IReadOnlyList<string> lines, IReadOnlyList<string> nonEmptyLines)
    {
        if (nonEmptyLines.Count == 0)
        {
            return null;
        }

        if (nonEmptyLines[0].Length > 80)
        {
            return null;
        }

        var titleSection = GetLeadingTitleSection(lines);

        var earlyCandidates = titleSection
            .Where(IsTitleCandidate)
            .ToList();

        var descriptiveCandidate = earlyCandidates
            .Where(line => line.Length >= 15)
            .OrderByDescending(line => line.Length)
            .FirstOrDefault();

        return descriptiveCandidate ?? earlyCandidates.FirstOrDefault() ?? nonEmptyLines[0];
    }

    private static IReadOnlyList<string> GetLeadingTitleSection(IReadOnlyList<string> lines)
    {
        var section = new List<string>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (section.Count > 0)
                {
                    break;
                }

                continue;
            }

            section.Add(line);

            if (section.Count >= 5)
            {
                break;
            }
        }

        return section;
    }

    private static List<FormattedBlock> BuildBlocks(IEnumerable<string> lines)
    {
        var blocks = new List<FormattedBlock>();
        var buffer = new List<string>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                FlushBuffer(blocks, buffer);
                continue;
            }

            if (IsListItem(line))
            {
                FlushBuffer(blocks, buffer);
                blocks.Add(new FormattedBlock(line, true));
                continue;
            }

            buffer.Add(line);
        }

        FlushBuffer(blocks, buffer);
        return blocks;
    }

    private static bool IsListItem(string line)
    {
        return line.StartsWith("- ") ||
               line.StartsWith("* ") ||
               line.StartsWith("• ") ||
               IsNumberedListItem(line);
    }

    private static bool IsNumberedListItem(string line)
    {
        var separatorIndex = line.IndexOf(". ", StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return false;
        }

        return int.TryParse(line[..separatorIndex], out _);
    }

    private static void FlushBuffer(List<FormattedBlock> blocks, List<string> buffer)
    {
        if (buffer.Count == 0)
        {
            return;
        }

        blocks.Add(new FormattedBlock(string.Join(" ", buffer), false));
        buffer.Clear();
    }

    internal sealed record FormattedPage(string Title, IReadOnlyList<FormattedBlock> Blocks);

    internal sealed record FormattedBlock(string Text, bool IsListItem);
}
