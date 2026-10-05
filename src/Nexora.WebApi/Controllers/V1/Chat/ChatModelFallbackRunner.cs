using System.Net;
using System.Text;
using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.WebApi.Controllers.V1.Chat;

public sealed record ChatGenerationResult(ChatModelChoice Model, string Answer);

public static class ChatModelFallbackRunner
{
    public static async Task<ChatGenerationResult> RunAsync(
        IReadOnlyList<ChatModelChoice> candidates,
        Func<ChatModelChoice, CancellationToken, Task<IAsyncEnumerable<string>>> startStream,
        Func<string, CancellationToken, Task> emitDelta,
        Func<ChatModelChoice, CancellationToken, Task> emitReset,
        Action<ChatModelChoice, ChatModelChoice, Exception> onFallback,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0) throw new ArgumentException("At least one model is required.", nameof(candidates));

        var provider = candidates[0].Provider;
        var providerCandidates = candidates
            .Where(candidate => string.Equals(candidate.Provider, provider, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        for (var index = 0; index < providerCandidates.Length; index++)
        {
            var model = providerCandidates[index];
            var output = new StringBuilder();
            try
            {
                var stream = await startStream(model, cancellationToken);
                await foreach (var delta in stream.WithCancellation(cancellationToken))
                {
                    output.Append(delta);
                    await emitDelta(delta, cancellationToken);
                }

                var answer = output.ToString().Trim();
                if (answer.Length == 0)
                    throw new HttpRequestException("The generation provider returned an empty answer.");

                return new ChatGenerationResult(model, answer);
            }
            catch (Exception exception) when (index + 1 < providerCandidates.Length && IsRetryable(exception, cancellationToken))
            {
                var nextModel = providerCandidates[index + 1];
                onFallback(model, nextModel, exception);
                if (output.Length > 0) await emitReset(nextModel, cancellationToken);
            }
        }

        throw new InvalidOperationException("No chat model produced an answer.");
    }

    private static bool IsRetryable(Exception exception, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return false;
        if (exception is TimeoutException) return true;
        if (exception is OperationCanceledException) return true;
        if (exception is not HttpRequestException requestException) return false;

        var statusCode = requestException.StatusCode;
        if (statusCode is null or HttpStatusCode.NotFound or HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests)
            return true;
        if ((int)statusCode >= 500) return true;

        var message = requestException.Message;
        return message.Contains("model", StringComparison.OrdinalIgnoreCase) &&
            (message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
             message.Contains("not available", StringComparison.OrdinalIgnoreCase) ||
             message.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
             message.Contains("unsupported", StringComparison.OrdinalIgnoreCase));
    }
}
