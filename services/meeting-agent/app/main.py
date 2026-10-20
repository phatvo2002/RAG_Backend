import asyncio
import logging
import signal

from .backend_client import BackendClient
from .config import Settings
from .room_runner import RoomRunner
from .transcriber import Transcriber


LOG = logging.getLogger(__name__)


async def run() -> None:
    settings = Settings.from_env()
    transcriber = Transcriber(settings.model_path)
    backend = BackendClient(settings)
    stopping = asyncio.Event()
    loop = asyncio.get_running_loop()
    for current in (signal.SIGINT, signal.SIGTERM):
        loop.add_signal_handler(current, stopping.set)
    runner = RoomRunner(backend, transcriber, settings.poll_seconds)
    try:
        while not stopping.is_set():
            try:
                jobs = await backend.jobs()
                if jobs:
                    if len(jobs) > 1:
                        LOG.warning("CPU worker capacity is one room; %s rooms are waiting", len(jobs) - 1)
                    await runner.run(jobs[0], stopping)
            except Exception:
                LOG.exception("Meeting job poll or room failed; retrying")
            try:
                await asyncio.wait_for(stopping.wait(), timeout=settings.poll_seconds)
            except asyncio.TimeoutError:
                pass
    finally:
        await backend.close()


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s %(message)s")
    asyncio.run(run())
