from dataclasses import dataclass, field
from datetime import timedelta
from typing import NewType, List

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
class Utterance:
    timestamp: TimestampRange
    text: str
    speaker_id: str
    confidence: ConfidenceScore
    words: List[Word] = field(default_factory=list)
