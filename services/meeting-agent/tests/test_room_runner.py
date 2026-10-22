import asyncio
from datetime import datetime, timedelta, timezone
import json
import unittest
from unittest.mock import patch
from uuid import uuid4

from app.models import Job, SpeechChunk
from app.room_runner import RoomRunner, TOPIC, speaker_id_from_identity


class FakeParticipant:
    def __init__(self):
        self.events = []

    async def publish_data(self, payload, **options):
        self.events.append((json.loads(payload), options))


class FakeRoom:
    def __init__(self):
        self.local_participant = FakeParticipant()
        self.remote_participants = {}
        self.handlers = {}
        self.connected = False
        self.disconnected = False

    def on(self, event, handler):
        self.handlers[event] = handler

    async def connect(self, url, token):
        self.connected = True

    async def disconnect(self):
        self.disconnected = True


class FakeBackend:
    def __init__(self):
        self.saved = []

    async def language(self, meeting_id, speaker_id):
        return "en"

    async def save_final(self, meeting_id, segment):
        self.saved.append(segment)
        return True

    async def jobs(self):
        return []


class FakeTranscriber:
    async def transcribe(self, chunk, language):
        return "Hello"


class SlowTranscriber:
    def __init__(self):
        self.started = asyncio.Event()
        self.release = asyncio.Event()

    async def transcribe(self, chunk, language):
        self.started.set()
        await self.release.wait()
        return f"Hello {chunk.sequence}"


class RoomRunnerTests(unittest.IsolatedAsyncioTestCase):
    async def test_completed_turn_is_processed_before_waiting_partial(self):
        meeting_id, first_speaker, second_speaker = uuid4(), uuid4(), uuid4()
        backend, room = FakeBackend(), FakeRoom()
        runner = RoomRunner(backend, FakeTranscriber())
        queue, partials, completed, wake = asyncio.Queue(), {}, set(), asyncio.Event()
        now = datetime.now(timezone.utc)
        partials[second_speaker] = (second_speaker, SpeechChunk(
            b"audio", now, now + timedelta(seconds=2), sequence=100))
        final = SpeechChunk(b"audio", now, now + timedelta(seconds=1), sequence=50)
        runner._enqueue(queue, first_speaker, final, meeting_id, partials, completed, wake)
        processor = asyncio.create_task(runner._process(
            Job(meeting_id, "room", "wss://example", "token"), room, queue, partials, completed, wake))
        for _ in range(100):
            if len(room.local_participant.events) == 2:
                break
            await asyncio.sleep(0.001)
        await queue.put(None)
        wake.set()
        await processor
        self.assertEqual(["final", "partial"], [event[0]["type"] for event in room.local_participant.events])
        self.assertEqual(first_speaker, backend.saved[0].speaker_user_id)

    async def test_new_snapshot_does_not_hide_in_flight_caption(self):
        meeting_id, speaker_id = uuid4(), uuid4()
        backend, room, transcriber = FakeBackend(), FakeRoom(), SlowTranscriber()
        runner = RoomRunner(backend, transcriber)
        queue, partials, completed, wake = asyncio.Queue(), {}, set(), asyncio.Event()
        now = datetime.now(timezone.utc)
        first = SpeechChunk(b"audio", now, now + timedelta(seconds=2), sequence=100)
        partials[speaker_id] = (speaker_id, first)
        wake.set()
        processor = asyncio.create_task(runner._process(
            Job(meeting_id, "room", "wss://example", "token"), room, queue, partials, completed, wake))
        await transcriber.started.wait()
        partials[speaker_id] = (speaker_id, SpeechChunk(
            b"audio", first.started_at, first.ended_at + timedelta(seconds=2), first.segment_id, 200))
        wake.set()
        transcriber.release.set()
        for _ in range(100):
            if len(room.local_participant.events) == 2:
                break
            await asyncio.sleep(0.001)
        await queue.put(None)
        wake.set()
        await processor
        self.assertEqual([100, 200], [event[0]["sequence"] for event in room.local_participant.events])

    async def test_partial_is_published_before_final_and_final_keeps_segment_id(self):
        meeting_id, speaker_id = uuid4(), uuid4()
        backend, room = FakeBackend(), FakeRoom()
        runner = RoomRunner(backend, FakeTranscriber())
        queue = asyncio.Queue()
        partials = {}
        completed = set()
        wake = asyncio.Event()
        now = datetime.now(timezone.utc)
        first = SpeechChunk(b"audio", now, now + timedelta(seconds=2), sequence=100)
        partials[speaker_id] = (speaker_id, first)
        wake.set()
        processor = asyncio.create_task(runner._process(
            Job(meeting_id, "room", "wss://example", "token"), room, queue, partials, completed, wake))
        for _ in range(100):
            if room.local_participant.events:
                break
            await asyncio.sleep(0.001)
        self.assertEqual("partial", room.local_participant.events[0][0]["type"])
        final = SpeechChunk(b"audio", first.started_at, first.ended_at + timedelta(seconds=1),
                            first.segment_id, 150)
        runner._enqueue(queue, speaker_id, final, meeting_id, partials, completed, wake)
        await queue.join()
        await queue.put(None)
        wake.set()
        await processor
        self.assertEqual(["partial", "final"], [event[0]["type"] for event in room.local_participant.events])
        self.assertEqual(1, len(backend.saved))
        self.assertEqual(first.segment_id, backend.saved[0].segment_id)

    async def test_publishes_saved_final_with_expected_topic_and_speaker(self):
        meeting_id = uuid4()
        speaker_id = uuid4()
        self.assertEqual(speaker_id, speaker_id_from_identity(speaker_id.hex, meeting_id))
        self.assertIsNone(speaker_id_from_identity(f"transcript-agent-{meeting_id.hex}", meeting_id))
        self.assertIsNone(speaker_id_from_identity("guest", meeting_id))
        backend = FakeBackend()
        room = FakeRoom()
        runner = RoomRunner(backend, FakeTranscriber())
        queue = asyncio.Queue()
        now = datetime.now(timezone.utc)
        await queue.put((speaker_id, SpeechChunk(b"audio", now, now + timedelta(seconds=1))))
        await queue.put(None)
        await runner._process(Job(meeting_id, "room", "wss://example", "token"), room, queue)
        await queue.join()
        self.assertEqual(1, len(backend.saved))
        event, options = room.local_participant.events[0]
        self.assertEqual("final", event["type"])
        self.assertEqual(str(speaker_id), event["speakerUserId"])
        self.assertEqual(str(backend.saved[0].segment_id), event["segmentId"])
        self.assertEqual("en", event["sourceLanguage"])
        self.assertEqual(TOPIC, options["topic"])
        self.assertTrue(options["reliable"])

    async def test_leaves_room_when_job_is_no_longer_active(self):
        from livekit import rtc

        meeting_id = uuid4()
        room = FakeRoom()
        runner = RoomRunner(FakeBackend(), FakeTranscriber(), poll_seconds=0.01)
        with patch.object(rtc, "Room", return_value=room):
            await runner.run(Job(meeting_id, "room", "wss://example", "token"), asyncio.Event())
        self.assertTrue(room.connected)
        self.assertTrue(room.disconnected)


if __name__ == "__main__":
    unittest.main()
