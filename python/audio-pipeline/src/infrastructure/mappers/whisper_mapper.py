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

            # Merge tokens into words 🧼🧩
            merged_words = []
            for token in words:
                if merged_words and not token.text.startswith(" "):
                    last_w = merged_words[-1]
                    new_text = last_w.text + token.text.strip()
                    new_end = token.timestamp.end
                    new_conf = (float(last_w.confidence) + float(token.confidence)) / 2

                    merged_words[-1] = Word(
                        text=new_text,
                        timestamp=TimestampRange(last_w.timestamp.start, new_end),
                        confidence=ConfidenceScore(new_conf),
                    )
                else:
                    merged_words.append(
                        Word(
                            text=token.text.strip(),
                            timestamp=token.timestamp,
                            confidence=token.confidence,
                        )
                    )

            # Return the segment as an Utterance bounded by its merged words! 📏🎯
            utterances.append(
                Utterance(
                    timestamp=TimestampRange(
                        start=merged_words[0].timestamp.start, end=merged_words[-1].timestamp.end
                    ),
                    text=" ".join([w.text for w in merged_words]),
                    speaker_id="Unknown",
                    confidence=ConfidenceScore(1.0),
                    words=merged_words,
                )
            )

        return utterances
