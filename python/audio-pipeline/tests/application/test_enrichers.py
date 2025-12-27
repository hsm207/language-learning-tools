from datetime import timedelta
from src.application.enrichers import SentenceSegmentationEnricher
from src.domain.value_objects import (
    Utterance,
    TimestampRange,
    Word,
    ConfidenceScore,
    LanguageTag,
)


def test_sentence_segmentation_enricher_splits_long_monologue():
    # Arrange
    # A long monologue (20 seconds) with two sentences
    words = [
        Word(
            "Hello",
            TimestampRange(timedelta(seconds=0), timedelta(seconds=1)),
            ConfidenceScore(1.0),
        ),
        Word(
            "world.",
            TimestampRange(timedelta(seconds=1), timedelta(seconds=2)),
            ConfidenceScore(1.0),
        ),
        Word(
            "This",
            TimestampRange(timedelta(seconds=11), timedelta(seconds=12)),
            ConfidenceScore(1.0),
        ),
        Word(
            "is",
            TimestampRange(timedelta(seconds=12), timedelta(seconds=13)),
            ConfidenceScore(1.0),
        ),
        Word(
            "long.",
            TimestampRange(timedelta(seconds=13), timedelta(seconds=14)),
            ConfidenceScore(1.0),
        ),
    ]

    utterance = Utterance(
        timestamp=TimestampRange(timedelta(seconds=0), timedelta(seconds=14)),
        text="Hello world. This is long.",
        speaker_id="Speaker1",
        confidence=ConfidenceScore(1.0),
        words=words,
    )

    # Max duration 1 second.
    # The first sentence "Hello world." ends at 2s.
    # 2s >= 1s, so it should split!
    enricher = SentenceSegmentationEnricher(max_duration_seconds=1.0)

    # Act
    enriched = enricher.enrich([utterance], LanguageTag("en-US"))

    # Assert
    assert len(enriched) == 2
    assert enriched[0].text == "Hello world."
    assert enriched[1].text == "This is long."
    assert enriched[0].timestamp.end == timedelta(seconds=2)
    assert enriched[1].timestamp.start == timedelta(seconds=11)


def test_sentence_segmentation_enricher_does_not_split_short_utterance():
    # Arrange
    utterance = Utterance(
        timestamp=TimestampRange(timedelta(seconds=0), timedelta(seconds=2)),
        text="Short.",
        speaker_id="Speaker1",
        confidence=ConfidenceScore(1.0),
        words=[],
    )
    enricher = SentenceSegmentationEnricher(max_duration_seconds=10.0)

    # Act
    enriched = enricher.enrich([utterance], LanguageTag("en-US"))

    # Assert
    assert len(enriched) == 1
    assert enriched[0] == utterance
