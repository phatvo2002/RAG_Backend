using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Docnet.Core;
using Docnet.Core.Converters;
using Docnet.Core.Models;
using Microsoft.Extensions.Options;
using Nexora.Infrastructure.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Nexora.Infrastructure.DocumentProcessing;

internal sealed record ExtractedDocumentText(string Text, string ParserName, string ParserVersion);

internal interface IDocumentTextExtractor
{
    Task<ExtractedDocumentText> ExtractAsync(string fileName, Stream content, CancellationToken cancellationToken);
}

internal sealed class DocumentTextExtractor(
    IOptions<DocumentProcessingOptions> options,
    IPdfOcrEngine ocrEngine) : IDocumentTextExtractor
{
    public async Task<ExtractedDocumentText> ExtractAsync(string fileName, Stream content, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName);
        return extension.ToLowerInvariant() switch
        {
            ".txt" or ".md" => new ExtractedDocumentText(await ReadTextAsync(content, cancellationToken), "text", "1"),
            ".docx" => new ExtractedDocumentText(await ExtractDocxAsync(content, cancellationToken), "openxml", "1"),
            ".pdf" => await ExtractPdfAsync(content, cancellationToken),
            _ => throw new InvalidOperationException("Định dạng tài liệu chưa được hỗ trợ.")
        };
    }

    private static async Task<string> ReadTextAsync(Stream content, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static async Task<string> ExtractDocxAsync(Stream content, CancellationToken cancellationToken)
    {
        using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.GetEntry("word/document.xml") ?? throw new InvalidOperationException("DOCX không chứa word/document.xml.");
        await using var stream = entry.Open();
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        XNamespace word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        return string.Join(Environment.NewLine, document.Descendants(word + "p").Select(paragraph => string.Concat(paragraph.Descendants(word + "t").Select(text => text.Value))).Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private async Task<ExtractedDocumentText> ExtractPdfAsync(Stream content, CancellationToken cancellationToken)
    {
        using var pdfBuffer = new MemoryStream();
        await content.CopyToAsync(pdfBuffer, cancellationToken);
        var pdfBytes = pdfBuffer.ToArray();

        using var document = PdfDocument.Open(pdfBytes);
        var text = string.Join(
            Environment.NewLine,
            document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page)));
        if (!string.IsNullOrWhiteSpace(text))
        {
            return new ExtractedDocumentText(text, "pdfpig", "0.1.16");
        }

        var dpi = options.Value.PdfOcrDpi;
        if (dpi <= 0)
        {
            throw new InvalidOperationException("DocumentProcessing:PdfOcrDpi phải lớn hơn 0.");
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"nexora-pdf-ocr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        try
        {
            using var docReader = DocLib.Instance.GetDocReader(pdfBytes, new PageDimensions(dpi / 72d));
            var pageTexts = new List<string>(docReader.GetPageCount());
            for (var pageIndex = 0; pageIndex < docReader.GetPageCount(); pageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var pageReader = docReader.GetPageReader(pageIndex);
                var image = pageReader.GetImage(new NaiveTransparencyRemover(255, 255, 255));
                var imagePath = Path.Combine(tempDirectory, $"page-{pageIndex + 1:D5}.bmp");
                await WriteBitmapAsync(imagePath, image, pageReader.GetPageWidth(), pageReader.GetPageHeight(), dpi, cancellationToken);
                pageTexts.Add(await ocrEngine.RecognizeAsync(imagePath, options.Value.TesseractPath, options.Value.OcrLanguage, dpi, cancellationToken));
            }

            return new ExtractedDocumentText(string.Join(Environment.NewLine, pageTexts), "pdfpig+docnet.core+tesseract", "1");
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    private static async Task WriteBitmapAsync(string path, byte[] bgra, int width, int height, int dpi, CancellationToken cancellationToken)
    {
        if (width <= 0 || height <= 0 || bgra.Length != checked(width * height * 4))
        {
            throw new InvalidOperationException("Docnet.Core đã trả về dữ liệu ảnh PDF không hợp lệ.");
        }

        var rowLength = checked(width * 3);
        var paddedRowLength = (rowLength + 3) & ~3;
        var pixelDataLength = checked(paddedRowLength * height);

        await using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(checked(54 + pixelDataLength));
        writer.Write(0);
        writer.Write(54);
        writer.Write(40);
        writer.Write(width);
        writer.Write(-height);
        writer.Write((short)1);
        writer.Write((short)24);
        writer.Write(0);
        writer.Write(pixelDataLength);
        var pixelsPerMeter = (int)Math.Round(dpi / 0.0254d);
        writer.Write(pixelsPerMeter);
        writer.Write(pixelsPerMeter);
        writer.Write(0);
        writer.Write(0);

        var padding = new byte[paddedRowLength - rowLength];
        for (var y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rowOffset = checked(y * width * 4);
            for (var x = 0; x < width; x++)
            {
                var pixelOffset = rowOffset + (x * 4);
                writer.Write(bgra[pixelOffset]);
                writer.Write(bgra[pixelOffset + 1]);
                writer.Write(bgra[pixelOffset + 2]);
            }

            writer.Write(padding);
        }

        await stream.FlushAsync(cancellationToken);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Keep the original extraction/OCR error as the processing failure.
        }
        catch (UnauthorizedAccessException)
        {
            // Keep the original extraction/OCR error as the processing failure.
        }
    }
}
