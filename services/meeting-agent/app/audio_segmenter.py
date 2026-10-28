from collections import deque
from datetime import datetime, timedelta
from uuid import UUID, uuid4

from .models import SpeechChunk


FRAME_MS = 20
FRAME_BYTES = 640  # 16 kHz, mono, signed 16-bit PCM
PRE_ROLL_FRAMES = 10
MIN_VOICE_FRAMES = 15
END_SILENCE_FRAMES = 25
MAX_FRAMES = 1000
PARTIAL_INTERVAL_FRAMES = 100


class AudioSegmenter:
    def __init__(self, vad=None):
        if vad is None:
            import webrtcvad
            vad = webrtcvad.Vad(3)
        self._vad = vad
        self._pre_roll: deque[tuple[bytes, datetime]] = deque(maxlen=PRE_ROLL_FRAMES)
        self._frames: list[tuple[bytes, datetime]] = []
        self._voice_frames = 0
        self._silence_frames = 0
        self._last_voice_end: datetime | None = None
        self._segment_id: UUID | None = None
        self._last_partial_frame = 0

    def advance(self, pcm: bytes, frame_end: datetime) -> SpeechChunk | None:
        if len(pcm) != FRAME_BYTES:
            raise ValueError("Audio frame must be 20 ms of 16 kHz mono PCM16")
        voiced = self._vad.is_speech(pcm, 16000)
        if not self._frames:
            if not voiced:
                self._pre_roll.append((pcm, frame_end))
                return None
            self._frames = list(self._pre_roll)
            self._pre_roll.clear()
            self._segment_id = uuid4()

        self._frames.append((pcm, frame_end))
        if voiced:
            self._voice_frames += 1
            self._silence_frames = 0
            self._last_voice_end = frame_end
        else:
            self._silence_frames += 1
        if self._silence_frames >= END_SILENCE_FRAMES or len(self._frames) >= MAX_FRAMES:
            return self.flush()
        return None

    def partial_snapshot(self) -> SpeechChunk | None:
        if (self._segment_id is None or self._last_voice_end is None or
                self._voice_frames < MIN_VOICE_FRAMES or
                len(self._frames) - self._last_partial_frame < PARTIAL_INTERVAL_FRAMES):
            return None
        self._last_partial_frame = len(self._frames)
        return SpeechChunk(
            b"".join(pcm for pcm, _ in self._frames),
            self._frames[0][1] - timedelta(milliseconds=FRAME_MS),
            self._last_voice_end,
            self._segment_id,
            len(self._frames),
        )

    def flush(self) -> SpeechChunk | None:
        frames = self._frames
        voice_frames = self._voice_frames
        last_voice_end = self._last_voice_end
        segment_id = self._segment_id
        self._frames = []
        self._voice_frames = 0
        self._silence_frames = 0
        self._last_voice_end = None
        self._segment_id = None
        self._last_partial_frame = 0
        if voice_frames < MIN_VOICE_FRAMES or last_voice_end is None or segment_id is None:
            return None
        return SpeechChunk(
            b"".join(pcm for pcm, _ in frames),
            frames[0][1] - timedelta(milliseconds=FRAME_MS),
            last_voice_end,
            segment_id,
            len(frames),
        )
