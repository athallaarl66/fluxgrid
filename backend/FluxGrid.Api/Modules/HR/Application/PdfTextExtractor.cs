using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace FluxGrid.Api.Modules.HR.Application;

public class PdfTextExtractor
{
    public string ExtractText(byte[] pdfBytes)
    {
        using var pdf = PdfDocument.Open(pdfBytes);
        var text = string.Join(Environment.NewLine,
            pdf.GetPages().Select(p => p.Text));
        return text;
    }

    public static bool IsScannedDocument(string extractedText) =>
        extractedText.Length < 50;
}
