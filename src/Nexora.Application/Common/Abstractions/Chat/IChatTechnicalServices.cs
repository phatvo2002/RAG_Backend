using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.Application.Common.Abstractions.Chat;

public interface IChatCipher
{
    byte[] Encrypt(string value);
    string Decrypt(byte[] value);
    string Hash(string value);
}

public interface IChatModelResolver
{
    ChatModelChoice Resolve(string id);
}
