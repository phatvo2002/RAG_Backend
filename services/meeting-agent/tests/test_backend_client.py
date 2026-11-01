import unittest
from uuid import uuid4

import httpx

from app.backend_client import BackendClient
from app.config import Settings


class BackendClientTests(unittest.IsolatedAsyncioTestCase):
    async def test_reads_translation_prompt_version_and_instructions(self):
        meeting_id = uuid4()
        version_id = str(uuid4())
        requests = []

        def handler(request: httpx.Request) -> httpx.Response:
            requests.append(request)
            return httpx.Response(200, json={"success": True, "data": {
                "versionId": version_id, "instructions": "Translate in a formal tone"
            }})

        async with httpx.AsyncClient(base_url="http://backend", transport=httpx.MockTransport(handler)) as client:
            backend = BackendClient(Settings(backend_url="http://backend", api_key="secret", model_path="/tmp/whisper"), client)
            self.assertEqual((version_id, "Translate in a formal tone"), await backend.translation_prompt(meeting_id))

        self.assertEqual(f"/internal/meetings/{meeting_id}/translation-prompt", requests[0].url.path)
        self.assertEqual("secret", requests[0].headers["X-Meeting-Agent-Key"])
