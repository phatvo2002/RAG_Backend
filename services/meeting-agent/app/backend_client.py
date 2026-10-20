import asyncio
import logging
from uuid import UUID

import httpx

from .config import Settings
from .models import Job, TranscriptSegment


LOG = logging.getLogger(__name__)


class BackendClient:
    def __init__(self, settings: Settings, client: httpx.AsyncClient | None = None):
        self._client = client or httpx.AsyncClient(base_url=settings.backend_url, timeout=10.0)
        self._owns_client = client is None
        self._headers = {"X-Meeting-Agent-Key": settings.api_key}

    async def close(self) -> None:
        if self._owns_client:
            await self._client.aclose()

    async def jobs(self) -> list[Job]:
        response = await self._client.get("/internal/meetings/transcription-jobs", headers=self._headers)
        response.raise_for_status()
        return [Job.from_json(item) for item in response.json()]

    async def language(self, meeting_id: UUID, speaker_id: UUID) -> str | None:
        url = f"/internal/meetings/{meeting_id}/speakers/{speaker_id}/source-language"
        for attempt in range(3):
            try:
                response = await self._client.get(url, headers=self._headers)
                if response.status_code in (403, 404):
                    return None
                if response.status_code < 500 and response.status_code != 429:
                    response.raise_for_status()
                    language = response.json()["data"]
                    return language if language in ("vi", "en") else None
            except httpx.RequestError:
                pass
            if attempt < 2:
                await asyncio.sleep(2 ** attempt)
        raise RuntimeError("Speaker language lookup failed after retries")

    async def save_final(self, meeting_id: UUID, segment: TranscriptSegment) -> bool:
        url = f"/internal/meetings/{meeting_id}/transcript/{segment.segment_id}"
        payload = segment.payload()
        for attempt in range(5):
            try:
                response = await self._client.put(url, headers=self._headers, json=payload)
                if response.status_code == 200:
                    return True
                if response.status_code < 500 and response.status_code != 429:
                    LOG.error("Transcript rejected: meeting=%s segment=%s status=%s", meeting_id, segment.segment_id, response.status_code)
                    return False
            except httpx.RequestError:
                pass
            if attempt < 4:
                await asyncio.sleep(2 ** attempt)
        LOG.error("Transcript save exhausted retries: meeting=%s segment=%s", meeting_id, segment.segment_id)
        return False
