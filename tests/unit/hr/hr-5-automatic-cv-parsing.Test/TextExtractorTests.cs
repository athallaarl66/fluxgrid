using FluxGrid.Api.Modules.HR.Application;

namespace FluxGrid.Api.Tests.HR;

public class TextExtractorTests
{
    private readonly PdfTextExtractor _pdfExtractor = new();
    private readonly DocxTextExtractor _docxExtractor = new();

    [Fact]
    public void IsScannedDocument_ReturnsTrue_WhenTextBelow50Chars()
    {
        Assert.True(PdfTextExtractor.IsScannedDocument("Short text"));
    }

    [Fact]
    public void IsScannedDocument_ReturnsFalse_WhenTextIs50OrMoreChars()
    {
        var text = new string('x', 50);
        Assert.False(PdfTextExtractor.IsScannedDocument(text));
    }

    [Fact]
    public void IsScannedDocument_ReturnsTrue_WhenTextIsEmpty()
    {
        Assert.True(PdfTextExtractor.IsScannedDocument(""));
    }
}
