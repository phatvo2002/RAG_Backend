import asyncio

import numpy as np

from .models import SpeechChunk


MIN_AUDIO_RMS = 0.003
MAX_NO_SPEECH_PROB = 0.5
MIN_AVG_LOGPROB = -0.8
MAX_COMPRESSION_RATIO = 2.4


class Transcriber:
    def __init__(self, model_path: str):
        from faster_whisper import WhisperModel
        self._model = WhisperModel(model_path, device="cpu", compute_type="int8", cpu_threads=4)

    async def transcribe(self, chunk: SpeechChunk, language: str) -> str:
        return await asyncio.to_thread(self._transcribe_sync, chunk.audio, language)

    def _transcribe_sync(self, pcm: bytes, language: str) -> str:
        samples = np.frombuffer(pcm, dtype="<i2").astype(np.float32) / 32768.0
        if not len(samples) or np.sqrt(np.mean(samples * samples)) < MIN_AUDIO_RMS:
            return ""
        segments, _ = self._model.transcribe(
            samples, language=language, task="transcribe", beam_size=1,
            condition_on_previous_text=False, vad_filter=False,
        )
        return " ".join(
            segment.text.strip() for segment in segments
            if (segment.text.strip() and segment.no_speech_prob < MAX_NO_SPEECH_PROB
                and segment.avg_logprob > MIN_AVG_LOGPROB
                and segment.compression_ratio < MAX_COMPRESSION_RATIO)
        ).strip()
