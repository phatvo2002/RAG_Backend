import unittest
from types import SimpleNamespace

import numpy as np

from app.transcriber import Transcriber


class FakeModel:
    def __init__(self, segments):
        self.segments = segments
        self.calls = 0

    def transcribe(self, samples, **kwargs):
        self.calls += 1
        return iter(self.segments), None


def segment(text, no_speech_prob=0.1, avg_logprob=-0.2, compression_ratio=1.0):
    return SimpleNamespace(text=text, no_speech_prob=no_speech_prob,
                           avg_logprob=avg_logprob, compression_ratio=compression_ratio)


class TranscriberTests(unittest.TestCase):
    def setUp(self):
        self.transcriber = Transcriber.__new__(Transcriber)

    def test_low_energy_audio_does_not_reach_whisper(self):
        self.transcriber._model = FakeModel([segment("Invented speech")])
        quiet = np.full(16000, 30, dtype="<i2").tobytes()
        self.assertEqual("", self.transcriber._transcribe_sync(quiet, "vi"))
        self.assertEqual(0, self.transcriber._model.calls)

    def test_unreliable_segments_are_not_published(self):
        self.transcriber._model = FakeModel([
            segment("No speech", no_speech_prob=0.8),
            segment("Low confidence", avg_logprob=-1.2),
            segment("Repeated text", compression_ratio=3.0),
            segment("Actual speech"),
        ])
        voice = np.full(16000, 1000, dtype="<i2").tobytes()
        self.assertEqual("Actual speech", self.transcriber._transcribe_sync(voice, "vi"))
        self.assertEqual(1, self.transcriber._model.calls)


if __name__ == "__main__":
    unittest.main()
