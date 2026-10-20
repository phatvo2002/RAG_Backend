from datetime import datetime, timedelta, timezone
import unittest
from unittest.mock import AsyncMock, patch
from uuid import uuid4

import httpx

from app.backend_client import BackendClient
from app.config import Settings
from app.models import SpeechChunk, TranscriptSegment


class BackendClientTests(unittest.IsolatedAsyncioTestCase):
    async def test_job_language_and_retry_keep_payload_and_key(self):
        meeting_id = uuid4()
        speaker_id = uuid4()
        requests = []

        def handler(request: httpx.Request) -> httpx.Response:
            requests.append(request)
            if request.url.path.endswith("transcription-jobs"):
                return httpx.Response(200, json=[{"meetingId": str(meeting_id), "roomName": "room",
                                                 "url": "wss://example", "token": "token"}])
            if request.url.path.endswith("source-language"):
                return httpx.Response(200, json={"data": "vi"})
            return httpx.Response(503 if sum(item.method == "PUT" for item in requests) == 1 else 200)

        async with httpx.AsyncClient(base_url="http://backend", transport=httpx.MockTransport(handler)) as transport:
            client = BackendClient(Settings("http://backend", "service-key", "/model"), transport)
            job = (await client.jobs())[0]
            self.assertEqual(meeting_id, job.meeting_id)
            self.assertEqual("vi", await client.language(meeting_id, speaker_id))
            now = datetime.now(timezone.utc)
            segment = TranscriptSegment.create(speaker_id, SpeechChunk(b"audio", now, now + timedelta(seconds=1)), "vi", "Xin chào")
            with patch("app.backend_client.asyncio.sleep", new_callable=AsyncMock):
                self.assertTrue(await client.save_final(meeting_id, segment))
        puts = [request for request in requests if request.method == "PUT"]
        self.assertEqual(2, len(puts))
        self.assertEqual(puts[0].url, puts[1].url)
        self.assertEqual(puts[0].content, puts[1].content)
        self.assertTrue(all(request.headers["X-Meeting-Agent-Key"] == "service-key" for request in requests))


if __name__ == "__main__":
    unittest.main()
