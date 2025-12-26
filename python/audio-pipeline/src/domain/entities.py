from dataclasses import dataclass, field
from enum import Enum, auto
from uuid import UUID, uuid4
from typing import List, Optional
from src.domain.value_objects import Utterance, LanguageTag

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
    utterances: List[Utterance] = field(default_factory=list)
    error_message: Optional[str] = None

    def mark_ingested(self):
        self.status = JobStatus.INGESTED

    def add_utterances(self, utterances: List[Utterance]):
        if not utterances:
            return
        self.utterances.extend(utterances)
        self.utterances.sort(key=lambda x: x.timestamp.start)
