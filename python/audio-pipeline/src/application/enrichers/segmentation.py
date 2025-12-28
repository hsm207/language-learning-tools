import re
import dataclasses
from typing import List
from src.domain.interfaces import IAudioEnricher, ILogger
from src.infrastructure.logging import NullLogger
from src.domain.value_objects import (
    Utterance,
    LanguageTag,
    TimestampRange,
    Word,
    ConfidenceScore,
)


class SentenceSegmentationEnricher(IAudioEnricher):
    """
    Splits long monologues into manageable rows that end with a complete sentence.
    Operates on atomic units (tokens/words) to ensure surgical precision! 🎯✂️
    """

    def __init__(
        self, max_duration_seconds: float = 15.0, logger: ILogger = NullLogger()
    ):
        self.max_duration_seconds = max_duration_seconds
        # Match tokens that end with terminal punctuation
        self.sentence_end_regex = re.compile(r".*[.!?]$")
        self.logger = logger

    def enrich(
        self, utterances: List[Utterance], language: LanguageTag
    ) -> List[Utterance]:
        final_utterances = []
        for u in utterances:
            # Recursively split long utterances! 🔄✂️
            final_utterances.extend(self._split_if_needed(u))
        return final_utterances

    def _split_if_needed(self, u: Utterance) -> List[Utterance]:
        """Simplified recursive splitting logic. 🧼💎"""
        if not u.words:
            return [u]

        # Always ensure tight bounding first 📏🎯
        u = dataclasses.replace(
            u,
            timestamp=TimestampRange(
                start=u.words[0].timestamp.start, end=u.words[-1].timestamp.end
            ),
        )

        duration = (u.timestamp.end - u.timestamp.start).total_seconds()

        # Base Case 1: Within threshold
        if duration <= self.max_duration_seconds:
            return [u]

        # Find potential split points (indices of terminal punctuation)
        split_indices = [
            i
            for i, w in enumerate(u.words)
            if self.sentence_end_regex.match(w.text.strip())
        ]

        # Filter out split points that are at the very end (can't split there!)
        valid_splits = [idx for idx in split_indices if idx < len(u.words) - 1]

        # Base Case 2: No terminal punctuation to split at
        if not valid_splits:
            self.logger.warning(
                f"Utterance too long ({duration:.2f}s) but no terminal punctuation found to split at: '{u.text[:50]}...'"
            )
            return [u]

        # Logic: Split at the FIRST terminal punctuation mark
        split_idx = valid_splits[0]

        part1_words = u.words[: split_idx + 1]
        part2_words = u.words[split_idx + 1 :]

        part1 = self._create_utterance_from_words(part1_words, u.speaker_id)
        part2 = self._create_utterance_from_words(part2_words, u.speaker_id)

        # Recurse! 🔄
        return self._split_if_needed(part1) + self._split_if_needed(part2)

    def _create_utterance_from_words(
        self, words: List[Word], speaker_id: str
    ) -> Utterance:
        text = "".join([w.text for w in words]).strip()
        start = words[0].timestamp.start
        end = words[-1].timestamp.end
        avg_confidence = (
            sum(float(w.confidence) for w in words) / len(words) if words else 1.0
        )

        return Utterance(
            timestamp=TimestampRange(start=start, end=end),
            text=text,
            speaker_id=speaker_id,
            confidence=ConfidenceScore(avg_confidence),
            words=words,
        )
