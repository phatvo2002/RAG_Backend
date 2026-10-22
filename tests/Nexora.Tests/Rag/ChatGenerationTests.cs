using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nexora.Application.Features.V1.Chat.Model;
using Nexora.Infrastructure.Services.Rag;

namespace Nexora.Tests.Rag;

public sealed class ChatGenerationTests
{
    [Fact]
    public void Available_model_order_uses_configured_gemini_first_and_claude_when_gemini_is_unavailable()
    {
        var streamer = new ChatModelStreamer(new HttpClient(), Options.Create(new ChatOptions
        {
            GeminiModel = "gemini-3.6-flash",
            ClaudeModel = "claude-test-model",
        }));

        var bothProviders = streamer.AvailableModels(hasGemini: true, hasClaude: true);
        var claudeOnly = streamer.AvailableModels(hasGemini: false, hasClaude: true);

        Assert.Equal("gemini", bothProviders[0].Id);
        Assert.Equal("gemini-3.6-flash", bothProviders[0].Model);
        Assert.Equal("claude", claudeOnly[0].Id);
        Assert.Equal("claude-test-model", claudeOnly[0].Model);
    }

    [Fact]
    public void Gemini_3_5_flash_lite_is_the_default_chat_model()
    {
        var streamer = new ChatModelStreamer(new HttpClient(), Options.Create(new ChatOptions()));

        var models = streamer.AvailableModels(hasGemini: true, hasClaude: false);

        Assert.Equal("gemini", models[0].Id);
        Assert.Equal("gemini-3.5-flash-lite", models[0].Model);
        Assert.Equal("Gemini 3.5 Flash Lite", models[0].Name);
        Assert.Contains(models, model => model.Model == "gemini-3.8-flash");
    }

    [Fact]
    public void Cipher_encrypts_and_decrypts_with_a_durable_key()
    {
        var options = Options.Create(new ChatOptions { EncryptionKeyBase64 = Convert.ToBase64String(Enumerable.Range(0, 32).Select(x => (byte)x).ToArray()) });
        var first = new ChatCipher(options);
        var encrypted = first.Encrypt("Xin chào, document");
        Assert.DoesNotContain("Xin chào", Encoding.UTF8.GetString(encrypted));
        Assert.Equal("Xin chào, document", new ChatCipher(options).Decrypt(encrypted));
        Assert.NotEqual(encrypted, first.Encrypt("Xin chào, document"));
    }

    [Fact]
    public void Missing_cipher_key_disables_chat_without_exposing_a_secret()
    {
        var cipher = new ChatCipher(Options.Create(new ChatOptions()));
        Assert.False(cipher.IsConfigured);
        Assert.Throws<InvalidOperationException>(() => cipher.Encrypt("question"));
    }

    [Fact]
    public async Task Gemini_uses_api_key_and_streams_text()
    {
        var handler = new RecordingHandler("event: step.delta\ndata: {\"event_type\":\"step.delta\",\"delta\":{\"type\":\"text\",\"text\":\"Hello\"}}\n\nevent: interaction.completed\ndata: {\"event_type\":\"interaction.completed\"}\n\n");
        var streamer = new ChatModelStreamer(new HttpClient(handler), Options.Create(new ChatOptions
        {
            GeminiModel = "test-gemini-model"
        }));
        var model = streamer.Resolve("gemini");
        var result = await CollectAsync(streamer.StreamAsync(model, [], "Question and sources", new ProviderCredential("test-gemini-key", "official", "https://generativelanguage.googleapis.com/v1beta"), CancellationToken.None));
        Assert.Equal("Hello", result);
        Assert.Equal("test-gemini-key", handler.ApiKey);
        Assert.Null(handler.Authorization);
        Assert.Equal("https://generativelanguage.googleapis.com/v1beta/interactions?alt=sse", handler.Url);
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("test-gemini-model", body.RootElement.GetProperty("model").GetString());
        Assert.Equal("Question and sources", body.RootElement.GetProperty("input").GetString());
        var systemInstruction = body.RootElement.GetProperty("system_instruction").GetString();
        Assert.Contains("general knowledge", systemInstruction);
        Assert.Contains("use only relevant authorized source passages", systemInstruction);
        Assert.Contains("inaccessible departments", systemInstruction);
        Assert.Equal(768, body.RootElement.GetProperty("generation_config").GetProperty("max_output_tokens").GetInt32());
        Assert.True(body.RootElement.GetProperty("stream").GetBoolean());
        Assert.False(body.RootElement.GetProperty("store").GetBoolean());
    }

    [Fact]
    public async Task Claude_uses_api_key_version_and_streams_text()
    {
        var handler = new RecordingHandler("event: content_block_delta\ndata: {\"type\":\"content_block_delta\",\"delta\":{\"type\":\"text_delta\",\"text\":\"Bonjour\"}}\n\n");
        var streamer = new ChatModelStreamer(new HttpClient(handler), Options.Create(new ChatOptions
        {
            ClaudeModel = "test-claude-model"
        }));
        var model = streamer.Resolve("claude");
        var result = await CollectAsync(streamer.StreamAsync(model, [], "Question and sources", new ProviderCredential("test-claude-key", "official", "https://api.anthropic.com/v1"), CancellationToken.None));
        Assert.Equal("Bonjour", result);
        Assert.Equal("test-claude-key", handler.ApiKey);
        Assert.Equal("2023-06-01", handler.Version);
        Assert.Null(handler.Authorization);
        Assert.Contains("test-claude-model", handler.Body);
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Contains("general knowledge", body.RootElement.GetProperty("system").GetString());
        Assert.Equal(768, body.RootElement.GetProperty("max_tokens").GetInt32());
    }

    [Fact]
    public async Task Provider_failure_does_not_yield_a_completed_answer()
    {
        var handler = new RecordingHandler("", HttpStatusCode.Unauthorized);
        var streamer = new ChatModelStreamer(new HttpClient(handler), Options.Create(new ChatOptions()));
        await Assert.ThrowsAsync<HttpRequestException>(() => CollectAsync(streamer.StreamAsync(streamer.Resolve("gemini"), [], "context", new ProviderCredential("key", "official", "https://generativelanguage.googleapis.com/v1beta"), CancellationToken.None)));
    }

    [Fact]
    public async Task Cancellation_stops_the_provider_request()
    {
        var streamer = new ChatModelStreamer(new HttpClient(new RecordingHandler("")),
            Options.Create(new ChatOptions()));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CollectAsync(streamer.StreamAsync(
            streamer.Resolve("gemini"), [], "context", new ProviderCredential("key", "official", "https://generativelanguage.googleapis.com/v1beta"), cancelled.Token)));
    }

    private static async Task<string> CollectAsync(IAsyncEnumerable<string> values)
    {
        var result = new StringBuilder();
        await foreach (var value in values) result.Append(value);
        return result.ToString();
    }

    private sealed class RecordingHandler(string sse, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? ApiKey { get; private set; }
        public string? Version { get; private set; }
        public string? Authorization { get; private set; }
        public string? Url { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ApiKey = request.Headers.TryGetValues("x-goog-api-key", out var gemini) ? gemini.Single() :
                request.Headers.TryGetValues("x-api-key", out var claude) ? claude.Single() : null;
            Version = request.Headers.TryGetValues("anthropic-version", out var versions) ? versions.Single() : null;
            Authorization = request.Headers.Authorization?.ToString();
            Url = request.RequestUri?.ToString();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(sse, Encoding.UTF8, "text/event-stream") };
        }
    }
}
