from typing import List
from datetime import timedelta
from src.domain.value_objects import (
    Utterance,
    TimestampRange,
    ConfidenceScore,
    Word,
)


class WhisperOutputMapper:
    """
    Maps Whisper JSON output to domain Utterance objects. 🎤🧩
    """

    def map(self, data: dict, audio_file_path: str = "") -> List[Utterance]:
        if not data or "transcription" not in data:
            return []

        utterances = []
        for segment in data.get("transcription", []):
            offsets = segment.get("offsets", {})
            seg_start = offsets.get("from", 0)
            seg_end = offsets.get("to", 0)

            words = []
            for token in segment.get("tokens", []):
                t_text = token.get("text", "")

                # Filter out Whisper control tokens
                if not t_text or t_text.strip().startswith("[_"):
                    continue

                t_offsets = token.get("offsets", {})
                t_start = t_offsets.get("from", seg_start)
                t_end = t_offsets.get("to", seg_end)
                t_conf = token.get("p", 1.0)

                words.append(
                    Word(
                        text=t_text,
                        timestamp=TimestampRange(
                            start=timedelta(milliseconds=t_start),
                            end=timedelta(milliseconds=t_end),
                        ),
                        confidence=ConfidenceScore(t_conf),
                    )
                )

            if not words:
                continue

            utterances.append(
                Utterance(
                    timestamp=TimestampRange(
                        start=words[0].timestamp.start, end=words[-1].timestamp.end
                    ),
                    text=segment.get("text", "").strip(),
                    speaker_id="Unknown",
                    confidence=ConfidenceScore(1.0),
                    words=words,
                )
            )

        return utterances
