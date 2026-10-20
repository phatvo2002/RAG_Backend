import asyncio
from datetime import datetime, timezone
import json
import logging
from uuid import UUID

from .audio_segmenter import AudioSegmenter
from .backend_client import BackendClient
from .models import Job, SpeechChunk, TranscriptSegment
from .transcriber import Transcriber


LOG = logging.getLogger(__name__)
TOPIC = "nexora.meeting.transcript.v1"


def speaker_id_from_identity(identity: str, meeting_id: UUID) -> UUID | None:
    if identity == f"transcript-agent-{meeting_id.hex}":
        return None
    try:
        return UUID(identity) if len(identity) == 32 else None
    except ValueError:
        return None


async def publish_final(room, segment: TranscriptSegment) -> None:
    await room.local_participant.publish_data(
        json.dumps(segment.event(), ensure_ascii=False), reliable=True, topic=TOPIC,
    )


class RoomRunner:
    def __init__(self, backend: BackendClient, transcriber: Transcriber, poll_seconds: float = 5.0):
        self._backend = backend
        self._transcriber = transcriber
        self._poll_seconds = poll_seconds

    async def run(self, job: Job, stopping: asyncio.Event) -> None:
        from livekit import rtc

        room = rtc.Room()
        disconnected = asyncio.Event()
        tracks: dict[str, asyncio.Task] = {}
        queue: asyncio.Queue[tuple[UUID, SpeechChunk] | None] = asyncio.Queue(maxsize=32)
        processor = asyncio.create_task(self._process(job, room, queue))

        def subscribed(track, publication, participant) -> None:
            if publication.source != rtc.TrackSource.SOURCE_MICROPHONE or publication.sid in tracks:
                return
            speaker_id = speaker_id_from_identity(participant.identity, job.meeting_id)
            if speaker_id is None:
                return
            tracks[publication.sid] = asyncio.create_task(
                self._read_track(job, publication.sid, track, speaker_id, queue)
            )

        def unsubscribed(_track, publication, _participant) -> None:
            task = tracks.pop(publication.sid, None)
            if task:
                task.cancel()

        room.on("track_subscribed", subscribed)
        room.on("track_unsubscribed", unsubscribed)
        room.on("disconnected", lambda *_: disconnected.set())
        try:
            await room.connect(job.url, job.token)
            LOG.info("Transcript worker joined meeting=%s", job.meeting_id)
            for participant in room.remote_participants.values():
                for publication in participant.track_publications.values():
                    if publication.track is not None:
                        subscribed(publication.track, publication, participant)
            while not stopping.is_set() and not disconnected.is_set():
                try:
                    await asyncio.wait_for(disconnected.wait(), timeout=self._poll_seconds)
                except asyncio.TimeoutError:
                    try:
                        jobs = await self._backend.jobs()
                        if not any(current.meeting_id == job.meeting_id for current in jobs):
                            break
                    except Exception:
                        LOG.warning("Could not refresh meeting job=%s; keeping room connection", job.meeting_id)
        finally:
            for task in tracks.values():
                task.cancel()
            if tracks:
                await asyncio.gather(*tracks.values(), return_exceptions=True)
            try:
                await asyncio.wait_for(queue.join(), timeout=30)
            except asyncio.TimeoutError:
                LOG.error("Transcript queue did not drain for meeting=%s", job.meeting_id)
                processor.cancel()
                await asyncio.gather(processor, return_exceptions=True)
            else:
                await queue.put(None)
                await processor
            await room.disconnect()
            LOG.info("Transcript worker left meeting=%s", job.meeting_id)

    async def _read_track(self, job: Job, track_sid: str, track, speaker_id: UUID,
                          queue: asyncio.Queue[tuple[UUID, SpeechChunk] | None]) -> None:
        from livekit import rtc

        stream = rtc.AudioStream.from_track(track=track, sample_rate=16000, num_channels=1,
                                            frame_size_ms=20, capacity=100)
        segmenter = AudioSegmenter()
        try:
            async for event in stream:
                chunk = segmenter.advance(bytes(event.frame.data), datetime.now(timezone.utc))
                if chunk:
                    self._enqueue(queue, speaker_id, chunk, job.meeting_id)
        except asyncio.CancelledError:
            raise
        except Exception:
            LOG.exception("Audio track failed: meeting=%s track=%s", job.meeting_id, track_sid)
        finally:
            chunk = segmenter.flush()
            if chunk:
                self._enqueue(queue, speaker_id, chunk, job.meeting_id)
            await stream.aclose()

    @staticmethod
    def _enqueue(queue: asyncio.Queue, speaker_id: UUID, chunk: SpeechChunk, meeting_id: UUID) -> None:
        try:
            queue.put_nowait((speaker_id, chunk))
        except asyncio.QueueFull:
            LOG.error("Transcript queue full: meeting=%s speaker=%s", meeting_id, speaker_id)

    async def _process(self, job: Job, room, queue: asyncio.Queue[tuple[UUID, SpeechChunk] | None]) -> None:
        while True:
            item = await queue.get()
            try:
                if item is None:
                    return
                speaker_id, chunk = item
                language = await self._backend.language(job.meeting_id, speaker_id)
                if language is None:
                    LOG.warning("No source language: meeting=%s speaker=%s", job.meeting_id, speaker_id)
                    continue
                text = (await self._transcriber.transcribe(chunk, language))[:5000]
                if not text:
                    continue
                segment = TranscriptSegment.create(speaker_id, chunk, language, text)
                if await self._backend.save_final(job.meeting_id, segment):
                    try:
                        await publish_final(room, segment)
                    except Exception:
                        LOG.warning("Could not publish transcript event: meeting=%s segment=%s", job.meeting_id, segment.segment_id)
            except Exception:
                LOG.exception("Transcript processing failed: meeting=%s", job.meeting_id)
            finally:
                queue.task_done()
