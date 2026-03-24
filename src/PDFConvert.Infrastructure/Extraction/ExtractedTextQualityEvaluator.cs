namespace PDFConvert.Infrastructure.Extraction;

internal static class ExtractedTextQualityEvaluator
{
    public static double Score(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0d;
        }

        var trimmed = text.Trim();
        var total = 0;
        var accepted = 0;
        var suspicious = 0;

        foreach (var character in trimmed)
        {
            if (char.IsWhiteSpace(character))
            {
                continue;
            }

            total++;

            if (IsAccepted(character))
            {
                accepted++;
            }

            if (IsSuspicious(character))
            {
                suspicious++;
            }
        }

        if (total == 0)
        {
            return 0d;
        }

        var acceptedRatio = (double)accepted / total;
        var suspiciousPenalty = (double)suspicious / total;
        return Math.Max(0d, acceptedRatio - suspiciousPenalty);
    }

    private static bool IsAccepted(char character)
    {
        return char.IsLetterOrDigit(character)
               || IsHangul(character)
               || character is '.' or ',' or ':' or ';' or '-' or '_' or '/' or '\\'
               or '(' or ')' or '[' or ']' or '{' or '}' or '&' or '+' or '%' or '#'
               or '*' or '!' or '?' or '\'' or '"';
    }

    private static bool IsSuspicious(char character)
    {
        return character is '�' or '□' or '■' or '▣' or '▤' or '▥' or '▦' or '▧'
               or '▨' or '▩' or '?' or '`';
    }

    private static bool IsHangul(char character)
    {
        return character is >= '\u1100' and <= '\u11FF'
               or >= '\u3130' and <= '\u318F'
               or >= '\uAC00' and <= '\uD7AF';
    }
}
