using System.Globalization;

namespace PDFConvert.Domain.ValueObjects;

public sealed record GitVersionInfo(int? CommitCount, string ShortHash)
{
    public static GitVersionInfo Unavailable { get; } = new(null, "---------");

    public string CommitCountDisplay => CommitCount?.ToString(CultureInfo.InvariantCulture) ?? "-";

    public string ShortHashDisplay => string.IsNullOrWhiteSpace(ShortHash) ? "---------" : ShortHash;

    public string StatusBarText => $"Commit Count: {CommitCountDisplay} | hash: {ShortHashDisplay}";
}
