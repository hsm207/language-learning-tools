from dataclasses import dataclass, field
from enum import Enum, auto
from uuid import UUID, uuid4
from typing import List, Optional
from src.domain.value_objects import Utterance, LanguageTag, AudioTranscript
from src.domain.events import (
    DomainEvent,
    AudioIngested,
    SpeechTranscribed,
    SpeakersIdentified,
    JobCompleted,
    JobFailed,
    EnrichmentStarted,
    PipelineStepTimed,
)


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
    _events: List[DomainEvent] = field(default_factory=list, compare=False, repr=False)

    def mark_ingested(self):
        self.status = JobStatus.INGESTED
        self.record_event(AudioIngested(job_id=self.id, source_path=self.source_path))

    def mark_transcribing(self):
        self.status = JobStatus.TRANSCRIBING

    def record_transcription_finished(self, count: int, lang: LanguageTag):
        self.record_event(
            SpeechTranscribed(job_id=self.id, utterance_count=count, language=lang)
        )

    def mark_diarizing(self):
        self.status = JobStatus.DIARIZING

    def record_diarization_finished(self, count: int):
        self.record_event(SpeakersIdentified(job_id=self.id, speaker_count=count))

    def mark_enriching(self, enricher_name: str = "Batch"):
        self.status = JobStatus.ENRICHING
        self.record_event(EnrichmentStarted(job_id=self.id, enricher_name=enricher_name))

    def record_step_duration(self, step_name: str, seconds: float):
        self.record_event(
            PipelineStepTimed(job_id=self.id, step_name=step_name, duration_seconds=seconds)
        )

    def complete(self, transcript: AudioTranscript):
        self.result = transcript
        self.status = JobStatus.COMPLETED
        self.record_event(
            JobCompleted(job_id=self.id, utterance_count=len(transcript.utterances))
        )

    def fail(self, error_message: str):
        self.status = JobStatus.FAILED
        self.error_message = error_message
        self.record_event(JobFailed(job_id=self.id, error_message=error_message))

    def record_event(self, event: DomainEvent):
        self._events.append(event)

    def pull_events(self) -> List[DomainEvent]:
        events = self._events[:]
        self._events.clear()
        return events

    @property
    def utterances(self) -> List[Utterance]:
        if not self.result:
            return []
        return self.result.utterances