from dataclasses import dataclass, field
from datetime import timedelta
from typing import NewType, List, Optional

LanguageTag = NewType("LanguageTag", str)
ConfidenceScore = NewType("ConfidenceScore", float)


@dataclass(frozen=True)
class TimestampRange:
    start: timedelta
    end: timedelta

    def __post_init__(self):
        if self.start > self.end:
            raise ValueError("Start time cannot be after end time!")


@dataclass(frozen=True)
class Word:
    text: str
    timestamp: TimestampRange
    confidence: ConfidenceScore


@dataclass(frozen=True)
class DiarizationOptions:
    num_speakers: Optional[int] = None
    min_speakers: Optional[int] = None
    max_speakers: Optional[int] = None


@dataclass(frozen=True)
class Utterance:
    timestamp: TimestampRange
    text: str
    speaker_id: str
    confidence: ConfidenceScore
    words: List[Word] = field(default_factory=list)
    translated_text: Optional[str] = None
    learner_notes: Optional[str] = None

    def __post_init__(self):
        for word in self.words:
            if (
                word.timestamp.start < self.timestamp.start
                or word.timestamp.end > self.timestamp.end
            ):
                raise ValueError(
                    f"Word '{word.text}' ({word.timestamp.start}-{word.timestamp.end}) "
                    f"falls outside utterance range ({self.timestamp.start}-{self.timestamp.end})!"
                )


@dataclass(frozen=True)
class AudioTranscript:
    """The high-fidelity 'Structured Output' of our pipeline!"""

    utterances: List[Utterance] = field(default_factory=list)
    target_language: Optional[LanguageTag] = None

    @property
    def speaker_ids(self) -> List[str]:
        return sorted(list(set(u.speaker_id for u in self.utterances)))

    @property
    def total_duration(self) -> timedelta:
        if not self.utterances:
            return timedelta(0)
        return self.utterances[-1].timestamp.end
