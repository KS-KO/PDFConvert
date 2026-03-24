using PDFConvert.Infrastructure.Extraction;

namespace PDFConvert.Tests;

public sealed class ExtractedTextQualityEvaluatorTests
{
    [Fact]
    public void Score_Returns_Higher_Value_For_Readable_Text()
    {
        var readable = "Laser safety architecture guide for wafer grooving";
        var garbled = "ASMQ<I/) ?쒖뒪??援ъ“ 諛??듭떖 ?덉쟾";

        var readableScore = ExtractedTextQualityEvaluator.Score(readable);
        var garbledScore = ExtractedTextQualityEvaluator.Score(garbled);

        Assert.True(readableScore > garbledScore);
    }
}
