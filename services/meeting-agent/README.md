# Meeting transcript worker

This is a separate Python/CPU process. It joins one active LiveKit room at a time, subscribes only to microphone tracks, and transcribes speech in the speaker's selected `vi` or `en` language. It does not record audio/video. Final transcript segments are translated with a local Ollama model for live captions.

## Run with Compose

Set `LiveKit__Url`, `LiveKit__ApiKey`, `LiveKit__ApiSecret`, and a strong `MeetingAgent__ApiKey` in the backend `.env`. The worker receives only the service key; WebApi issues its short-lived LiveKit room token. Apply the existing `AddMeetingLifecycle` and `AddMeetingTranscript` migrations through the normal deployment process. From the backend directory:

```sh
docker compose build meeting-agent
docker compose up -d ollama
docker compose exec ollama ollama pull qwen3.5:4b
docker compose up -d webapi meeting-agent
docker compose logs -f meeting-agent
```

The service appears in the default Compose stack. `docker compose up -d meeting-agent` starts it together with its WebApi dependency; start Ollama separately before translation. Use `docker compose create meeting-agent` to register the container without starting stopped services. Pull the translation model before using live translation; Ollama keeps it in the `ollama_models` volume. To switch models, pull the desired model, set `OLLAMA_TRANSLATION_MODEL` in `.env`, then recreate meeting-agent without rebuilding its image. The Ollama API is reachable only inside the Compose network.

The Docker build downloads the multilingual faster-whisper `small` model into the image. Build needs internet access; runtime does not download model weights. The worker is configured with `MEETING_AGENT_BACKEND_URL=http://webapi:8080`, `MEETING_AGENT_API_KEY`, and `WHISPER_MODEL_PATH=/opt/models/whisper-small`. `docker compose config --quiet` checks variable substitution before starting.

## Data flow

1. Poll `GET /internal/meetings/transcription-jobs` every five seconds. Use the first active meeting; log a capacity warning while others wait.
2. Connect to LiveKit with the job token and subscribe to each participant's microphone as 16 kHz mono PCM. The LiveKit identity is the participant's GUID in `N` format. Other tracks and the worker identity are ignored.
3. Group frames into turns with aggressive WebRTC VAD and require at least 300 ms of detected speech. Reject very quiet audio before inference and discard Whisper segments with high no-speech probability, low confidence, or excessive repetition. While a person speaks, take a cumulative audio snapshot at most every two seconds and transcribe it with faster-whisper `small` on `cpu/int8`. Publish the current text as `type: partial` on `nexora.meeting.transcript.v1`. The same segment ID and increasing sequence identify updates to one turn. Only one inference runs at a time; stale partial snapshots are dropped and completed turns take priority.
4. After a turn ends, transcribe it once more and save the final segment through the protected backend API. Only after a successful save, publish it with `type: final` and the same segment ID. The browser also refetches stored finals, so it can recover missed packets. Partial text is never persisted.
5. Queue the saved final for translation to the opposite language. A separate, bounded task calls Ollama with `MEETING_AGENT_OLLAMA_URL` and `OLLAMA_TRANSLATION_MODEL` (default `qwen3.5:4b`) and publishes `type: translation`, `segmentId`, `targetLanguage`, and `text` on the same LiveKit topic. Translation is not persisted. If Ollama is unavailable or overloaded, original captions continue. Each viewer chooses `vi` or `en` independently; captions show the translation when available and retain the original for comparison.

Backend writes are retried with the same ID up to five times. A failed turn is logged by IDs only; audio is never persisted, so a worker crash or exhausted retries can lose that turn. The inference queue is bounded to prevent unlimited memory growth on an overloaded CPU. A second room waits for the first to finish. Run only one worker replica: the current job endpoint has no lease or claim mechanism to prevent duplicate workers.

Partial recognition repeatedly processes the audio accumulated in a turn (up to 20 seconds), so CPU use can be several times higher than final-only transcription. Logs report audio duration, inference wall time, process CPU time, queue depth, and caption delay by IDs without recording text. Measure these on the deployment CPU before increasing the update frequency.

Ollama and Whisper may compete for CPU and RAM. Measure translation quality, latency and memory use with real Vietnamese and English speech before wider rollout. The agent logs translation latency and queue depth without logging transcript text.

## Tests and acceptance

Install `requirements.txt` in a Python 3.11 environment, then run `python -m unittest discover -s tests -v` from this directory. For end-to-end verification, start a meeting with two authorized accounts, select different spoken languages, speak for more than five seconds, and confirm partial captions appear on the speaker's video before the pause. Confirm the saved final replaces the partial and remains in the panel after reload. Check camera-off placeholders, screen sharing, pinning, and the caption toggle. Stop the worker to confirm the call still works without captions. Measure first-caption latency and CPU load on the actual host.
