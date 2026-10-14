# Nexora backend

The backend is a .NET 10 solution. Meeting transcription runs in a separate .NET worker; the frontend remains React/TypeScript. The worker communicates with the WebApi and LiveKit over the network and has no project reference to Domain, Application, or Infrastructure.

## Local development

Copy `src/Nexora.WebApi/secrets.example.json` to `src/Nexora.WebApi/secrets.local.json` and fill in the local credentials. The local file is ignored by Git, excluded from the Docker build context, and excluded from .NET build/publish output. WebApi and EF Core design-time tooling read it after the tracked `appsettings` files; environment variables can override its values. Services receive the resulting configuration through `IConfiguration` or their existing options bindings. Never commit or share the local file.

For the local MinIO and Qdrant containers, copy `.env.example` to `.env`. Set `MINIO_ROOT_USER` and `MINIO_ROOT_PASSWORD` to the same values as `Storage:AccessKey` and `Storage:SecretKey` in `secrets.local.json`, then run `docker compose up -d` from the repository root. The Compose data is stored in Docker named volumes and remains available after `docker compose down`. Keep `.env` local and update it when rotating the storage credentials.

Configure any other services required by the WebApi using local environment variables or .NET user secrets. Set the same `MeetingAgent__ApiKey` on the WebApi and worker. Keep the LiveKit API key and secret on the WebApi only. Apply pending EF Core migrations to your own development PostgreSQL database before using transcript features; this repository change does not apply them automatically.

From the repository root:

```powershell
dotnet run --project src\Nexora.WebApi --launch-profile http
```

In another terminal, download the Whisper and Silero models once, then configure their absolute paths and start the worker:

```powershell
cd services\meeting-agent-dotnet\Nexora.MeetingAgent
dotnet run -- --download-models
```

See [the worker guide](services/meeting-agent-dotnet/README.md) for its environment variables and run command. Start the React frontend with `npm run dev` from `D:\RAG\my_rag-frontend`.

The meeting transcription service is implemented only by the .NET worker. Verify two simultaneous microphone tracks and reliable data packets in a live LiveKit room before relying on live captions.

## Production secrets

Create `/etc/nexora/webapi-secrets.json` on the production host with the same JSON keys as the example file and production values, including `ConnectionStrings:Postgres`. Mount it read-only at `/app/secrets.local.json` in the WebApi container (for example, a Compose volume mapping of `/etc/nexora/webapi-secrets.json:/app/secrets.local.json:ro`). Make sure the container's application user can read the host file. Do not add this file to the image or repository.

The following credentials were previously committed in `appsettings` and should be replaced at their providers:

- PostgreSQL passwords in the Development and Production connection strings.
- `Jwt:SignaturePrivateKey` (replacement invalidates tokens signed with the old key).
- `Storage:AccessKey` and `Storage:SecretKey`.
- `Embedding:ApiKey`.
- `LiveKit:ApiKey` and `LiveKit:ApiSecret` as a matching pair.

Preserve `ProviderKeys:EncryptionKeyBase64` until existing stored provider keys have been re-encrypted with a replacement key; changing it immediately would prevent decryption of those values. Existing Git history still contains the old credentials, so removing them from current files does not revoke them.
