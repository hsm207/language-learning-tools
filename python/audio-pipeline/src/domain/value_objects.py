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
            raise ValueError("Start time cannot be after end time! 💖")

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

    def __post_init__(self):
        # Strict invariant: words must be within the utterance's time range! 📏💎
        for word in self.words:
            if word.timestamp.start < self.timestamp.start or word.timestamp.end > self.timestamp.end:
                raise ValueError(
                    f"Word '{word.text}' ({word.timestamp.start}-{word.timestamp.end}) "
                    f"falls outside utterance range ({self.timestamp.start}-{self.timestamp.end})! 💖"
                )
