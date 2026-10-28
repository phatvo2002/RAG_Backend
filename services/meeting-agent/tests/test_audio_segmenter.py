import unittest
from datetime import datetime, timedelta, timezone

from app.audio_segmenter import AudioSegmenter, FRAME_BYTES


class FakeVad:
    def is_speech(self, pcm: bytes, sample_rate: int) -> bool:
        assert sample_rate == 16000
        return pcm[0] == 1


class AudioSegmenterTests(unittest.TestCase):
    def test_partial_before_pause_uses_same_segment_as_final(self):
        segmenter = AudioSegmenter(FakeVad())
        now = datetime(2026, 1, 1, tzinfo=timezone.utc)
        partials = []
        for index in range(260):
            frame_end = now + timedelta(milliseconds=(index + 1) * 20)
            self.assertIsNone(segmenter.advance(bytes([1]) * FRAME_BYTES, frame_end))
            snapshot = segmenter.partial_snapshot()
            if snapshot:
                partials.append(snapshot)
        self.assertEqual(2, len(partials))
        self.assertLess(partials[0].sequence, partials[1].sequence)
        self.assertEqual(partials[0].segment_id, partials[1].segment_id)
        final = None
        for index in range(25):
            final = segmenter.advance(bytes(FRAME_BYTES), now + timedelta(milliseconds=(261 + index) * 20))
        self.assertIsNotNone(final)
        self.assertEqual(partials[0].segment_id, final.segment_id)
        self.assertGreater(final.sequence, partials[-1].sequence)

    def test_two_tracks_produce_separate_turns(self):
        first = AudioSegmenter(FakeVad())
        second = AudioSegmenter(FakeVad())
        start = datetime(2026, 1, 1, tzinfo=timezone.utc)
        results = [[], []]
        for index in range(45):
            end = start + timedelta(milliseconds=(index + 1) * 20)
            for track, begins, output in ((first, 0, results[0]), (second, 5, results[1])):
                voiced = begins <= index < begins + 15
                pcm = bytes([1 if voiced else 0]) * FRAME_BYTES
                chunk = track.advance(pcm, end)
                if chunk:
                    output.append(chunk)
        self.assertEqual([1, 1], [len(items) for items in results])
        self.assertLess(results[0][0].ended_at, results[1][0].ended_at)
        self.assertEqual(start + timedelta(milliseconds=300), results[0][0].ended_at)
        self.assertEqual(start + timedelta(milliseconds=400), results[1][0].ended_at)

    def test_short_noise_is_discarded_and_disconnect_flushes_speech(self):
        segmenter = AudioSegmenter(FakeVad())
        now = datetime(2026, 1, 1, tzinfo=timezone.utc)
        for index in range(4):
            segmenter.advance(bytes([1]) * FRAME_BYTES, now + timedelta(milliseconds=(index + 1) * 20))
        self.assertIsNone(segmenter.flush())
        for index in range(15):
            segmenter.advance(bytes([1]) * FRAME_BYTES, now + timedelta(seconds=1, milliseconds=(index + 1) * 20))
        self.assertIsNotNone(segmenter.flush())

    def test_voice_shorter_than_300ms_is_discarded(self):
        segmenter = AudioSegmenter(FakeVad())
        now = datetime(2026, 1, 1, tzinfo=timezone.utc)
        for index in range(14):
            segmenter.advance(bytes([1]) * FRAME_BYTES, now + timedelta(milliseconds=(index + 1) * 20))
        self.assertIsNone(segmenter.flush())


if __name__ == "__main__":
    unittest.main()
