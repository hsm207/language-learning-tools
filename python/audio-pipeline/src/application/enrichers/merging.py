import dataclasses
from typing import List
from src.domain.interfaces import IAudioEnricher
from src.domain.value_objects import (
    Utterance,
    LanguageTag,
    Word,
    TimestampRange,
    ConfidenceScore,
)


class TokenMergerEnricher(IAudioEnricher):
    """
    Merges Whisper sub-word tokens into whole human-readable words. 🧩💎
    Runs after segmentation to ensure final rows are clean! 🧼⚖️
    """

    def enrich(
        self, utterances: List[Utterance], language: LanguageTag
    ) -> List[Utterance]:
        enriched = []
        for u in utterances:
            if not u.words:
                enriched.append(u)
                continue

            merged_words = []
            for token in u.words:
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

            new_text = " ".join([w.text for w in merged_words])
            new_u = dataclasses.replace(u, words=merged_words, text=new_text)
            enriched.append(new_u)

        return enriched
