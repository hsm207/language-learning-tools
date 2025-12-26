from dataclasses import dataclass
from datetime import datetime
from uuid import UUID
from src.domain.value_objects import LanguageTag

@dataclass(frozen=True)
class DomainEvent:
    occurred_at: datetime = datetime.now()

@dataclass(frozen=True)
class AudioIngested(DomainEvent):
    job_id: UUID
    source_path: str
    duration_seconds: float

@dataclass(frozen=True)
class SpeechTranscribed(DomainEvent):
    job_id: UUID
    transcript_path: str
    language: LanguageTag

@dataclass(frozen=True)
class SpeakersIdentified(DomainEvent):
    job_id: UUID
    speaker_count: int

@dataclass(frozen=True)
class SubtitleReady(DomainEvent):
    job_id: UUID
    output_path: str
