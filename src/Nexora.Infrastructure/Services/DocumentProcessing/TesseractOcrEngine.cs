using System.ComponentModel;
using System.Diagnostics;

namespace Nexora.Infrastructure.Services.DocumentProcessing;

internal interface IPdfOcrEngine
{
    Task<string> RecognizeAsync(string imagePath, string executablePath, string language, int dpi, CancellationToken cancellationToken);
}

internal sealed class TesseractOcrEngine : IPdfOcrEngine
{
    public async Task<string> RecognizeAsync(string imagePath, string executablePath, string language, int dpi, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add(imagePath);
        process.StartInfo.ArgumentList.Add("stdout");
        process.StartInfo.ArgumentList.Add("-l");
        process.StartInfo.ArgumentList.Add(language);
        process.StartInfo.ArgumentList.Add("--dpi");
        process.StartInfo.ArgumentList.Add(dpi.ToString(System.Globalization.CultureInfo.InvariantCulture));

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Không thể khởi chạy Tesseract để nhận dạng PDF.");
            }
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            throw new InvalidOperationException($"Không tìm thấy công cụ Tesseract '{executablePath}'.", exception);
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }

            throw;
        }

        var output = await outputTask;
        var error = await errorTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Tesseract xử lý trang PDF thất bại: {error}");
        }

        return output;
    }
}
