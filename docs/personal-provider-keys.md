# Personal AI provider keys

Each signed-in user can save one Gemini key and one Claude key at `/account`. The account page never receives the stored key again. PostgreSQL stores AES-256-GCM ciphertext and a SHA-256 fingerprint; the server must retain its separate encryption key to decrypt saved credentials after a restart.

## Deployment

1. Set `ProviderKeys__EncryptionKeyBase64` in the server's secret manager or environment to a stable Base64-encoded 32-byte random value. Back it up securely. Do not put it in `appsettings.json` or source control. Rotating it requires re-encrypting saved keys; losing it requires users to replace their keys.
2. Apply the `AddUserProviderKeys` EF Core migration, then synchronize the API function catalog (or apply the matching seed script) before opening the account page.
3. Configure optional approved Claude gateways through `ProviderKeys:ClaudeEndpoints`, mapping a short ID to an HTTPS base URL ending at `/v1`. The built-in `official` endpoint always points to `https://api.anthropic.com/v1`. Users select an ID; they cannot supply a URL.
4. Rotate any previously exposed shared provider keys. Chat no longer reads `Chat:GeminiApiKey`, `Chat:ClaudeApiKey`, or `Chat:ClaudeBaseUrl`. Document and question embeddings continue using `Embedding:ApiKey`.

For PowerShell 7+, generate the server encryption secret with `[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))`. On Windows PowerShell 5.1, use `$bytes = New-Object byte[] 32; $rng = [Security.Cryptography.RandomNumberGenerator]::Create(); $rng.GetBytes($bytes); $rng.Dispose(); [Convert]::ToBase64String($bytes)`.

For local development, save that value outside source control with `dotnet user-secrets set "ProviderKeys:EncryptionKeyBase64" "<generated-value>" --project .\src\Nexora.WebApi`. Apply the migration with `dotnet ef database update --project .\src\Nexora.Infrastructure --startup-project .\src\Nexora.WebApi --context NexoraDbContext` after confirming the target connection string.

Saving a provider key checks its shape and endpoint selection without calling the provider. An invalid or expired key is reported when the user sends a chat message. A request already streaming uses the credential resolved at its start; replacement or deletion affects subsequent requests.
