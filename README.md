# Nexora backend

The backend is a .NET 10 solution. Meeting transcription runs in a separate .NET worker; the frontend remains React/TypeScript. The worker communicates with the WebApi and LiveKit over the network and has no project reference to Domain, Application, or Infrastructure.

## Local development

Configure PostgreSQL, LiveKit, and the other services required by the WebApi using local environment variables or .NET user secrets. Set the same `MeetingAgent__ApiKey` on the WebApi and worker. Keep the LiveKit API key and secret on the WebApi only. Apply pending EF Core migrations to your own development PostgreSQL database before using transcript features; this repository change does not apply them automatically.

From `D:\RAG\my_rag-backend`:

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
