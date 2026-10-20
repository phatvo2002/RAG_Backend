from dataclasses import dataclass
import os


@dataclass(frozen=True)
class Settings:
    backend_url: str
    api_key: str
    model_path: str
    poll_seconds: float = 5.0

    @classmethod
    def from_env(cls) -> "Settings":
        backend_url = os.getenv("MEETING_AGENT_BACKEND_URL", "http://webapi:8080").rstrip("/")
        api_key = os.getenv("MEETING_AGENT_API_KEY", "")
        model_path = os.getenv("WHISPER_MODEL_PATH", "/opt/models/whisper-small")
        if not backend_url.startswith(("http://", "https://")) or not api_key:
            raise ValueError("MEETING_AGENT_BACKEND_URL and MEETING_AGENT_API_KEY are required")
        return cls(backend_url, api_key, model_path)
