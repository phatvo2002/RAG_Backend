import unittest
from datetime import datetime, timedelta, timezone

from app.audio_segmenter import AudioSegmenter, FRAME_BYTES


class FakeVad:
    def is_speech(self, pcm: bytes, sample_rate: int) -> bool:
        assert sample_rate == 16000
        return pcm[0] == 1


class AudioSegmenterTests(unittest.TestCase):
    def test_two_tracks_produce_separate_turns(self):
        first = AudioSegmenter(FakeVad())
        second = AudioSegmenter(FakeVad())
        start = datetime(2026, 1, 1, tzinfo=timezone.utc)
        results = [[], []]
        for index in range(45):
            end = start + timedelta(milliseconds=(index + 1) * 20)
            for track, begins, output in ((first, 0, results[0]), (second, 5, results[1])):
                voiced = begins <= index < begins + 10
                pcm = bytes([1 if voiced else 0]) * FRAME_BYTES
                chunk = track.advance(pcm, end)
                if chunk:
                    output.append(chunk)
        self.assertEqual([1, 1], [len(items) for items in results])
        self.assertLess(results[0][0].ended_at, results[1][0].ended_at)
        self.assertEqual(start + timedelta(milliseconds=200), results[0][0].ended_at)
        self.assertEqual(start + timedelta(milliseconds=300), results[1][0].ended_at)

    def test_short_noise_is_discarded_and_disconnect_flushes_speech(self):
        segmenter = AudioSegmenter(FakeVad())
        now = datetime(2026, 1, 1, tzinfo=timezone.utc)
        for index in range(4):
            segmenter.advance(bytes([1]) * FRAME_BYTES, now + timedelta(milliseconds=(index + 1) * 20))
        self.assertIsNone(segmenter.flush())
        for index in range(10):
            segmenter.advance(bytes([1]) * FRAME_BYTES, now + timedelta(seconds=1, milliseconds=(index + 1) * 20))
        self.assertIsNotNone(segmenter.flush())


if __name__ == "__main__":
    unittest.main()
