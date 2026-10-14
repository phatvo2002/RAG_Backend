using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Options;
using Nexora.Infrastructure.Services.DocumentProcessing;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Nexora.Tests.Documents;

public sealed class DocumentTextExtractorTests
{
    [Fact]
    public async Task ExtractAsync_DigitalPdf_UsesPdfPigWithoutOcr()
    {
        var pdf = CreatePdf("Digital PDF text");
        var ocr = new RecordingPdfOcrEngine();
        var extractor = CreateExtractor(ocr);

        var result = await extractor.ExtractAsync("digital.pdf", new MemoryStream(pdf), CancellationToken.None);

        Assert.Contains("Digital PDF text", result.Text);
        Assert.Equal("pdfpig", result.ParserName);
        Assert.Equal("0.1.16", result.ParserVersion);
        Assert.Empty(ocr.Calls);
    }

    [Fact]
    public async Task ExtractAsync_ImageOnlyPdf_RendersPagesAndOcrsThemInOrder()
    {
        var pdf = CreatePdf(null, null);
        var ocr = new RecordingPdfOcrEngine();
        var extractor = CreateExtractor(ocr);
        var existingDirectories = Directory.GetDirectories(Path.GetTempPath(), "nexora-pdf-ocr-*");

        var result = await extractor.ExtractAsync("scanned.pdf", new MemoryStream(pdf), CancellationToken.None);

        Assert.Equal("OCR page 1" + Environment.NewLine + "OCR page 2", result.Text);
        Assert.Equal("pdfpig+docnet.core+tesseract", result.ParserName);
        Assert.Equal("1", result.ParserVersion);
        Assert.Equal(2, ocr.Calls.Count);
        Assert.All(ocr.Calls, call =>
        {
            Assert.Equal("tesseract", call.ExecutablePath);
            Assert.Equal("vie+eng", call.Language);
            Assert.Equal(200, call.Dpi);
            Assert.Equal("BM", call.ImageHeader);
        });
        Assert.Empty(Directory.GetDirectories(Path.GetTempPath(), "nexora-pdf-ocr-*").Except(existingDirectories));
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".md")]
    public async Task ExtractAsync_TextFile_PreservesTextPath(string extension)
    {
        var ocr = new RecordingPdfOcrEngine();
        var extractor = CreateExtractor(ocr);

        var result = await extractor.ExtractAsync($"note{extension}", new MemoryStream(Encoding.UTF8.GetBytes("plain text")), CancellationToken.None);

        Assert.Equal("plain text", result.Text);
        Assert.Equal("text", result.ParserName);
        Assert.Empty(ocr.Calls);
    }

    [Fact]
    public async Task ExtractAsync_Docx_PreservesOpenXmlPath()
    {
        var ocr = new RecordingPdfOcrEngine();
        var extractor = CreateExtractor(ocr);

        var result = await extractor.ExtractAsync("note.docx", CreateDocx("Word text"), CancellationToken.None);

        Assert.Equal("Word text", result.Text);
        Assert.Equal("openxml", result.ParserName);
        Assert.Empty(ocr.Calls);
    }

    [Fact]
    public async Task ExtractAsync_EmptyOcrOutput_ReturnsEmptyTextForExistingProcessingFailureHandling()
    {
        var ocr = new RecordingPdfOcrEngine { ReturnEmptyText = true };
        var extractor = CreateExtractor(ocr);

        var result = await extractor.ExtractAsync("scanned.pdf", new MemoryStream(CreatePdf((string?)null)), CancellationToken.None);

        Assert.Equal(string.Empty, result.Text);
        Assert.Single(ocr.Calls);
    }

    [Fact]
    public async Task ExtractAsync_OcrFailure_PropagatesAndCleansTemporaryImages()
    {
        var ocr = new RecordingPdfOcrEngine { ExceptionToThrow = new InvalidOperationException("Tesseract failed.") };
        var extractor = CreateExtractor(ocr);
        var existingDirectories = Directory.GetDirectories(Path.GetTempPath(), "nexora-pdf-ocr-*");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            extractor.ExtractAsync("scanned.pdf", new MemoryStream(CreatePdf((string?)null)), CancellationToken.None));

        Assert.Equal("Tesseract failed.", exception.Message);
        Assert.Empty(Directory.GetDirectories(Path.GetTempPath(), "nexora-pdf-ocr-*").Except(existingDirectories));
    }

    [Fact]
    public async Task ExtractAsync_OcrCancellation_PropagatesAndCleansTemporaryImages()
    {
        using var cancellation = new CancellationTokenSource();
        var ocr = new RecordingPdfOcrEngine { CancellationToTrigger = cancellation };
        var extractor = CreateExtractor(ocr);
        var existingDirectories = Directory.GetDirectories(Path.GetTempPath(), "nexora-pdf-ocr-*");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            extractor.ExtractAsync("scanned.pdf", new MemoryStream(CreatePdf((string?)null)), cancellation.Token));

        Assert.Empty(Directory.GetDirectories(Path.GetTempPath(), "nexora-pdf-ocr-*").Except(existingDirectories));
    }

    [Fact]
    public async Task TesseractOcrEngine_NonzeroExit_UsesExistingProcessingFailurePath()
    {
        var executable = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.SystemDirectory, "cmd.exe")
            : File.Exists("/usr/bin/false") ? "/usr/bin/false" : "/bin/false";
        if (!File.Exists(executable))
        {
            return;
        }

        var imageArgument = OperatingSystem.IsWindows() ? "/c" : "unused-image.bmp";
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TesseractOcrEngine().RecognizeAsync(imageArgument, executable, "vie+eng", 200, CancellationToken.None));

        Assert.Contains("Tesseract xử lý trang PDF thất bại", exception.Message);
    }

    private static DocumentTextExtractor CreateExtractor(RecordingPdfOcrEngine ocr) =>
        new(Options.Create(new DocumentProcessingOptions()), ocr);

    private static byte[] CreatePdf(params string?[] pageTexts)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        foreach (var pageText in pageTexts)
        {
            var page = builder.AddPage(595, 842);
            if (pageText is not null)
            {
                page.AddText(pageText, 12, new PdfPoint(50, 790), font);
            }
        }

        return builder.Build();
    }

    private static MemoryStream CreateDocx(string paragraphText)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        using (var writer = new StreamWriter(archive.CreateEntry("word/document.xml").Open()))
        {
            writer.Write($"<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body><w:p><w:r><w:t>{paragraphText}</w:t></w:r></w:p></w:body></w:document>");
        }

        stream.Position = 0;
        return stream;
    }

    private sealed class RecordingPdfOcrEngine : IPdfOcrEngine
    {
        public List<OcrCall> Calls { get; } = [];
        public bool ReturnEmptyText { get; init; }
        public Exception? ExceptionToThrow { get; init; }
        public CancellationTokenSource? CancellationToTrigger { get; init; }

        public Task<string> RecognizeAsync(string imagePath, string executablePath, string language, int dpi, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var imageHeader = Encoding.ASCII.GetString(File.ReadAllBytes(imagePath), 0, 2);
            Calls.Add(new OcrCall(imagePath, executablePath, language, dpi, imageHeader));
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            if (CancellationToTrigger is not null)
            {
                CancellationToTrigger.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }

            return Task.FromResult(ReturnEmptyText ? string.Empty : $"OCR page {Calls.Count}");
        }
    }

    private sealed record OcrCall(string ImagePath, string ExecutablePath, string Language, int Dpi, string ImageHeader);
}
