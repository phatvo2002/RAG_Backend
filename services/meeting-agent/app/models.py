from dataclasses import dataclass
from datetime import datetime, timezone
from uuid import UUID, uuid4


@dataclass(frozen=True)
class Job:
    meeting_id: UUID
    room_name: str
    url: str
    token: str

    @classmethod
    def from_json(cls, value: dict) -> "Job":
        return cls(UUID(value["meetingId"]), value["roomName"], value["url"], value["token"])


@dataclass(frozen=True)
class SpeechChunk:
    audio: bytes
    started_at: datetime
    ended_at: datetime


@dataclass(frozen=True)
class TranscriptSegment:
    segment_id: UUID
    speaker_user_id: UUID
    started_at: datetime
    ended_at: datetime
    source_language: str
    text: str

    @classmethod
    def create(cls, speaker_user_id: UUID, chunk: SpeechChunk, language: str, text: str) -> "TranscriptSegment":
        return cls(uuid4(), speaker_user_id, chunk.started_at, chunk.ended_at, language, text)

    def payload(self) -> dict:
        def iso(value: datetime) -> str:
            return value.astimezone(timezone.utc).isoformat().replace("+00:00", "Z")

        return {
            "segmentId": str(self.segment_id),
            "speakerUserId": str(self.speaker_user_id),
            "startedAt": iso(self.started_at),
            "endedAt": iso(self.ended_at),
            "sourceLanguage": self.source_language,
            "text": self.text,
        }

    def event(self) -> dict:
        return {"type": "final", **self.payload()}
