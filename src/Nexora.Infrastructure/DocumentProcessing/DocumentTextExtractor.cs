using System.Diagnostics;
using System.ComponentModel;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Nexora.Infrastructure.Options;

namespace Nexora.Infrastructure.DocumentProcessing;

internal sealed record ExtractedDocumentText(string Text, string ParserName, string ParserVersion);

internal interface IDocumentTextExtractor
{
    Task<ExtractedDocumentText> ExtractAsync(string fileName, Stream content, CancellationToken cancellationToken);
}

internal sealed class DocumentTextExtractor(IOptions<DocumentProcessingOptions> options) : IDocumentTextExtractor
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
        var inputPath = Path.GetTempFileName() + ".pdf";
        var textPath = Path.GetTempFileName() + ".txt";
        var ocrPath = Path.GetTempFileName() + ".pdf";
        try
        {
            await using (var file = File.Create(inputPath))
            {
                await content.CopyToAsync(file, cancellationToken);
            }

            await RunProcessAsync(options.Value.PdfToTextPath, ["-layout", inputPath, textPath], cancellationToken);
            var text = await File.ReadAllTextAsync(textPath, cancellationToken);
            if (!string.IsNullOrWhiteSpace(text))
            {
                return new ExtractedDocumentText(text, "pdftotext", "1");
            }

            await RunProcessAsync(options.Value.OcrmyPdfPath, ["--skip-text", "--deskew", "--language", options.Value.OcrLanguage, inputPath, ocrPath], cancellationToken);
            await RunProcessAsync(options.Value.PdfToTextPath, ["-layout", ocrPath, textPath], cancellationToken);
            text = await File.ReadAllTextAsync(textPath, cancellationToken);
            return new ExtractedDocumentText(text, "ocrmypdf+tesseract", "1");
        }
        finally
        {
            DeleteIfExists(inputPath);
            DeleteIfExists(textPath);
            DeleteIfExists(ocrPath);
        }
    }

    private static async Task RunProcessAsync(string fileName, IReadOnlyCollection<string> arguments, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException($"Không thể khởi chạy công cụ xử lý tài liệu: {fileName}.");
            }
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            throw new InvalidOperationException($"Không tìm thấy công cụ xử lý tài liệu '{fileName}'. Hãy cài pdftotext và OCRmyPDF/Tesseract.", exception);
        }

        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException($"Công cụ '{fileName}' xử lý thất bại: {error}");
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
