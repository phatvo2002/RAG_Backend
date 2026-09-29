using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Nexora.Infrastructure.Options;

namespace Nexora.Infrastructure.Rag;

public sealed class ChatCipher
{
    private readonly byte[]? _key;

    public ChatCipher(IOptions<ChatOptions> options)
    {
        try
        {
            _key = Convert.FromBase64String(options.Value.EncryptionKeyBase64);
        }
        catch (FormatException)
        {
            _key = null;
        }
    }

    public bool IsConfigured => _key?.Length == 32;

    private byte[] Key => IsConfigured ? _key! : throw new InvalidOperationException("Chat:EncryptionKeyBase64 must be a base64-encoded 32-byte secret.");

    public byte[] Encrypt(string value)
    {
        var plaintext = Encoding.UTF8.GetBytes(value);
        var output = new byte[1 + 12 + 16 + plaintext.Length];
        output[0] = 1;
        var nonce = output.AsSpan(1, 12);
        RandomNumberGenerator.Fill(nonce);
        using var aes = new AesGcm(Key, 16);
        aes.Encrypt(nonce, plaintext, output.AsSpan(29), output.AsSpan(13, 16));
        return output;
    }

    public string Decrypt(byte[] value)
    {
        if (value.Length < 29 || value[0] != 1)
        {
            throw new CryptographicException("Unsupported chat ciphertext format.");
        }

        var plaintext = new byte[value.Length - 29];
        using var aes = new AesGcm(Key, 16);
        aes.Decrypt(value.AsSpan(1, 12), value.AsSpan(29), value.AsSpan(13, 16), plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }

    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
