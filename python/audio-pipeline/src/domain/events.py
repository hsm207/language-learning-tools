import inspect
import os
from dataclasses import dataclass, field
from datetime import datetime
from uuid import UUID
from typing import Optional
from src.domain.value_objects import LanguageTag


@dataclass(frozen=True, kw_only=True)
class DomainEvent:
    occurred_at: datetime = field(default_factory=datetime.now)
    origin_file: str = field(init=False)
    origin_line: int = field(init=False)

    def __post_init__(self):
        # Reach back in the stack to find the first frame that isn't
        # in events.py OR entities.py to find the real application origin! 🕵️‍♀️🔬✨
        stack = inspect.stack()

        # We ignore frames from these files to find the 'True Caller'
        ignored_files = ["events.py", "entities.py", "contextlib.py", "abc.py"]

        for frame_info in stack:
            filename = os.path.basename(frame_info.filename)
            if filename not in ignored_files and not filename.startswith("<"):
                object.__setattr__(self, "origin_file", filename)
                object.__setattr__(self, "origin_line", frame_info.lineno)
                break
        else:
            object.__setattr__(self, "origin_file", "unknown")
            object.__setattr__(self, "origin_line", 0)


@dataclass(frozen=True, kw_only=True)
class AudioIngested(DomainEvent):
    job_id: UUID
    source_path: str


@dataclass(frozen=True, kw_only=True)
class SpeechTranscribed(DomainEvent):
    job_id: UUID
    utterance_count: int
    language: LanguageTag


@dataclass(frozen=True, kw_only=True)
class SpeakersIdentified(DomainEvent):
    job_id: UUID
    speaker_count: int


@dataclass(frozen=True, kw_only=True)
class EnrichmentStarted(DomainEvent):
    job_id: UUID
    enricher_name: str


@dataclass(frozen=True, kw_only=True)
class PipelineStepTimed(DomainEvent):
    """Captures the performance of a SOTA component. ⏱️✨"""

    job_id: UUID
    step_name: str
    duration_seconds: float


@dataclass(frozen=True, kw_only=True)
class JobCompleted(DomainEvent):
    job_id: UUID
    utterance_count: int


@dataclass(frozen=True, kw_only=True)
class JobFailed(DomainEvent):
    job_id: UUID
    error_message: str
