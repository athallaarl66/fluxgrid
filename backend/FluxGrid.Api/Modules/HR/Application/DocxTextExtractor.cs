using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace FluxGrid.Api.Modules.HR.Application;

public class DocxTextExtractor
{
    public string ExtractText(byte[] docxBytes)
    {
        using var ms = new MemoryStream(docxBytes);
        using var wordDoc = WordprocessingDocument.Open(ms, false);
        var body = wordDoc.MainDocumentPart?.Document?.Body;
        if (body is null) return string.Empty;

        var texts = body.Descendants<Text>()
            .Where(t => t.Text != null)
            .Select(t => t.Text);
        return string.Join(Environment.NewLine, texts);
    }
}
