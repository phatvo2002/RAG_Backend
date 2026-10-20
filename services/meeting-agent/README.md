# Meeting transcript worker

This is a separate Python/CPU process. It joins one active LiveKit room at a time, subscribes only to microphone tracks, and transcribes each completed speech turn in the speaker's selected `vi` or `en` language. It does not record audio/video or translate speech.

## Run with Compose

Set `LiveKit__Url`, `LiveKit__ApiKey`, `LiveKit__ApiSecret`, and a strong `MeetingAgent__ApiKey` in the backend `.env`. The worker receives only the service key; WebApi issues its short-lived LiveKit room token. Apply the existing `AddMeetingLifecycle` and `AddMeetingTranscript` migrations through the normal deployment process. From the backend directory:

```sh
docker compose build meeting-agent
docker compose up -d webapi meeting-agent
docker compose logs -f meeting-agent
```

The service appears in the default Compose stack. `docker compose up -d meeting-agent` starts it together with its WebApi dependency; use `docker compose create meeting-agent` to register the container without starting stopped services.

The Docker build downloads the multilingual faster-whisper `small` model into the image. Build needs internet access; runtime does not download model weights. The worker is configured with `MEETING_AGENT_BACKEND_URL=http://webapi:8080`, `MEETING_AGENT_API_KEY`, and `WHISPER_MODEL_PATH=/opt/models/whisper-small`. `docker compose config --quiet` checks variable substitution before starting.

## Data flow

1. Poll `GET /internal/meetings/transcription-jobs` every five seconds. Use the first active meeting; log a capacity warning while others wait.
2. Connect to LiveKit with the job token and subscribe to each participant's microphone as 16 kHz mono PCM. The LiveKit identity is the participant's GUID in `N` format. Other tracks and the worker identity are ignored.
3. Group frames into turns with WebRTC VAD. After a turn, request the current speaker language, run faster-whisper `small` with `cpu/int8`, and create one stable segment ID.
4. Save the final segment through the protected backend API. Only after a successful save, publish the same fields with `type: final` to the LiveKit data packet topic `nexora.meeting.transcript.v1`. The browser also refetches stored transcript, so it can recover missed packets.

Backend writes are retried with the same ID up to five times. A failed turn is logged by IDs only; audio is never persisted, so a worker crash or exhausted retries can lose that turn. The inference queue is bounded to prevent unlimited memory growth on an overloaded CPU. A second room waits for the first to finish. Run only one worker replica: the current job endpoint has no lease or claim mechanism to prevent duplicate workers.

## Tests and acceptance

Install `requirements.txt` in a Python 3.11 environment, then run `python -m unittest discover -s tests -v` from this directory. For end-to-end verification, start a meeting with two authorized accounts, select different spoken languages, speak in turns, and confirm correct names/languages in the live panel and after reload. Stop the worker to confirm the call still works without captions. Measure turn-end-to-caption latency on the actual CPU host; the model and host determine performance.
