import asyncio

import numpy as np

from .models import SpeechChunk


class Transcriber:
    def __init__(self, model_path: str):
        from faster_whisper import WhisperModel
        self._model = WhisperModel(model_path, device="cpu", compute_type="int8", cpu_threads=4)

    async def transcribe(self, chunk: SpeechChunk, language: str) -> str:
        return await asyncio.to_thread(self._transcribe_sync, chunk.audio, language)

    def _transcribe_sync(self, pcm: bytes, language: str) -> str:
        samples = np.frombuffer(pcm, dtype="<i2").astype(np.float32) / 32768.0
        segments, _ = self._model.transcribe(
            samples, language=language, task="transcribe", beam_size=1,
            condition_on_previous_text=False, vad_filter=False,
        )
        return " ".join(segment.text.strip() for segment in segments if segment.text.strip()).strip()
