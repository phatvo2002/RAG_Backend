using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Nexora.Infrastructure.Options;

namespace Nexora.Infrastructure.Rag;

public sealed class ProviderKeyCipher(IOptions<ProviderKeyOptions> options)
{
    public bool IsConfigured
    {
        get
        {
            try { return Convert.FromBase64String(options.Value.EncryptionKeyBase64).Length == 32; }
            catch (FormatException) { return false; }
        }
    }

    private byte[] Key
    {
        get
        {
            byte[] key;
            try { key = Convert.FromBase64String(options.Value.EncryptionKeyBase64); }
            catch (FormatException) { throw new InvalidOperationException("ProviderKeys:EncryptionKeyBase64 must be a base64-encoded 32-byte secret."); }
            if (key.Length != 32) throw new InvalidOperationException("ProviderKeys:EncryptionKeyBase64 must be a base64-encoded 32-byte secret.");
            return key;
        }
    }

    private static byte[] AssociatedData(Guid userId, string provider) => Encoding.UTF8.GetBytes($"{userId:N}:{provider}");

    public byte[] Encrypt(Guid userId, string provider, string value)
    {
        var plaintext = Encoding.UTF8.GetBytes(value);
        var result = new byte[1 + 12 + 16 + plaintext.Length];
        result[0] = 1;
        var nonce = result.AsSpan(1, 12);
        RandomNumberGenerator.Fill(nonce);
        using var aes = new AesGcm(Key, 16);
        aes.Encrypt(nonce, plaintext, result.AsSpan(29), result.AsSpan(13, 16), AssociatedData(userId, provider));
        CryptographicOperations.ZeroMemory(plaintext);
        return result;
    }

    public string Decrypt(Guid userId, string provider, byte[] value)
    {
        if (value.Length < 29 || value[0] != 1) throw new CryptographicException("Unsupported provider key ciphertext format.");
        var plaintext = new byte[value.Length - 29];
        using var aes = new AesGcm(Key, 16);
        aes.Decrypt(value.AsSpan(1, 12), value.AsSpan(29), value.AsSpan(13, 16), plaintext, AssociatedData(userId, provider));
        var text = Encoding.UTF8.GetString(plaintext);
        CryptographicOperations.ZeroMemory(plaintext);
        return text;
    }
}
