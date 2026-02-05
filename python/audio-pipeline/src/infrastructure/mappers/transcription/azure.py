from typing import List
from datetime import timedelta
from src.domain.value_objects import (
    Utterance,
    TimestampRange,
    ConfidenceScore,
    Word,
)


class AzureTranscriptionMapper:
    """
    Maps Azure AI Speech JSON output to domain Utterance objects. ☁️🏷️
    """

    def map(self, data: dict, **kwargs) -> List[Utterance]:
        utterances = []
        for phrase in data.get("phrases", []):
            offset_ms = phrase.get("offsetMilliseconds", 0)
            duration_ms = phrase.get("durationMilliseconds", 0)
            speaker_id = str(phrase.get("speaker", "Unknown"))

            words = []
            for word_data in phrase["words"]:
                w_offset = word_data.get("offsetMilliseconds", offset_ms)
                w_duration = word_data.get("durationMilliseconds", 0)
                words.append(
                    Word(
                        text=word_data.get("text", ""),
                        timestamp=TimestampRange(
                            start=timedelta(milliseconds=w_offset),
                            end=timedelta(milliseconds=w_offset + w_duration),
                        ),
                        confidence=ConfidenceScore(word_data.get("confidence", 1.0)),
                    )
                )

            utterances.append(
                Utterance(
                    timestamp=TimestampRange(
                        start=timedelta(milliseconds=offset_ms),
                        end=timedelta(milliseconds=offset_ms + duration_ms),
                    ),
                    text=phrase.get("text", ""),
                    speaker_id=speaker_id,
                    confidence=ConfidenceScore(phrase.get("confidence", 1.0)),
                    words=words,
                )
            )

        return utterances
