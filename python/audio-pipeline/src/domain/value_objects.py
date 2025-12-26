from dataclasses import dataclass
from datetime import timedelta
from typing import NewType

LanguageTag = NewType("LanguageTag", str)
ConfidenceScore = NewType("ConfidenceScore", float)

@dataclass(frozen=True)
class TimestampRange:
    start: timedelta
    end: timedelta

    def __post_init__(self):
        if self.start > self.end:
            raise ValueError("Start time cannot be after end time, silly! 💖")

@dataclass(frozen=True)
class Utterance:
    timestamp: TimestampRange
    text: str
    speaker_id: str
    confidence: ConfidenceScore
