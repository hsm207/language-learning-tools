from dataclasses import dataclass, field
from enum import Enum, auto
from uuid import UUID, uuid4
from typing import List, Optional
from src.domain.value_objects import Utterance, LanguageTag, AudioTranscript

class JobStatus(Enum):
    CREATED = auto()
    INGESTED = auto()
    TRANSCRIBING = auto()
    DIARIZING = auto()
    ENRICHING = auto()
    COMPLETED = auto()
    FAILED = auto()

@dataclass
class AudioArtifact:
    id: UUID = field(default_factory=uuid4)
    file_path: str = ""
    format: str = ""
    sample_rate: int = 16000

@dataclass
class ProcessingJob:
    id: UUID = field(default_factory=uuid4)
    source_path: str = ""
    target_language: LanguageTag = LanguageTag("de-DE")
    status: JobStatus = JobStatus.CREATED
    result: Optional[AudioTranscript] = None
    error_message: Optional[str] = None

    def mark_ingested(self):
        self.status = JobStatus.INGESTED

    def complete(self, transcript: AudioTranscript):
        self.result = transcript
        self.status = JobStatus.COMPLETED

    @property
    def utterances(self) -> List[Utterance]:
        if not self.result:
            return []
        return self.result.utterances
