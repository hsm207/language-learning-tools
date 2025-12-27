import json
import os
import pytest
import logging
from datetime import timedelta
from src.application.enrichers import SentenceSegmentationEnricher
from src.infrastructure.logging import StandardLogger
from src.domain.value_objects import (
    Utterance,
    Word,
    TimestampRange,
    ConfidenceScore,
    LanguageTag,
)

# --- TEST CONSTANTS (Derived from test_30s_raw.json analysis) ---
HALLO_START_MS = 80
FIRST_PERIOD_END_MS = 1060
LAST_WORD_END_MS = 6770
LOOSE_SEGMENT_START_MS = 0
LOOSE_SEGMENT_END_MS = 6780


@pytest.fixture
def raw_utterances():
    """Fixture to load raw Whisper JSON data using LOOSE segment bounds. 🏗️📉"""
    json_path = os.path.join(os.path.dirname(__file__), "../data/test_30s_raw.json")
    with open(json_path, "r", encoding="utf-8") as f:
        data = json.load(f)

    utterances = []
    for segment in data.get("transcription", []):
        offsets = segment.get("offsets", {})
        seg_start = offsets.get("from", 0)
        seg_end = offsets.get("to", 0)

        words = []
        for token in segment.get("tokens", []):
            t_text = token.get("text", "")
            if not t_text or t_text.strip().startswith("[_"):
                continue

            t_offsets = token.get("offsets", {})
            t_start = t_offsets.get("from", seg_start)
            t_end = t_offsets.get("to", seg_end)
            t_p = token.get("p", 1.0)

            words.append(
                Word(
                    text=t_text,
                    timestamp=TimestampRange(
                        timedelta(milliseconds=t_start), timedelta(milliseconds=t_end)
                    ),
                    confidence=ConfidenceScore(t_p),
                )
            )

        if words:
            # Load with the original LOOSE segment bounds 🌬️🚫
            utterances.append(
                Utterance(
                    timestamp=TimestampRange(
                        timedelta(milliseconds=seg_start),
                        timedelta(milliseconds=seg_end),
                    ),
                    text=segment.get("text", "").strip(),
                    speaker_id="SPEAKER_00",
                    confidence=ConfidenceScore(1.0),
                    words=words,
                )
            )
    return utterances


def test_utterance_snapping_behavior(raw_utterances):
    """
    STRICTLY verifies that loose segment bounds are snapped tight to word tokens. 📏⚡️
    """
    enricher = SentenceSegmentationEnricher(max_duration_seconds=100.0)
    enriched = enricher.enrich(raw_utterances, LanguageTag("de"))

    u0 = enriched[0]
    expected_start = timedelta(milliseconds=HALLO_START_MS)
    expected_end = timedelta(milliseconds=LAST_WORD_END_MS)

    assert u0.timestamp.start == expected_start
    assert u0.timestamp.end == expected_end


def test_recursive_segmentation_splits_at_punctuation(raw_utterances, caplog):
    """
    STRICTLY verifies that segments are split correctly, bounds are surgical,
    and warnings are thrown for un-splittable long rows! ✂️🔄🔊
    """
    # Arrange
    THRESHOLD = 3.0
    # Inject a StandardLogger so caplog works! 👂💎⚖️
    logger = StandardLogger(name="SentenceSegmentationEnricher")
    enricher = SentenceSegmentationEnricher(max_duration_seconds=THRESHOLD, logger=logger)

    # Act
    with caplog.at_level(logging.WARNING):
        enriched = enricher.enrich(raw_utterances, LanguageTag("de"))

    # Assert: Segment 0 was split into Part 1 and Part 2
    row0 = enriched[0]
    row1 = enriched[1]

    # 1. Row 0 Assertions (Should be perfectly within threshold) 📏✅
    assert row0.text == "Hallo und herzlich willkommen."
    assert row0.timestamp.start == timedelta(milliseconds=HALLO_START_MS)
    assert row0.timestamp.end == timedelta(milliseconds=FIRST_PERIOD_END_MS)

    row0_duration = (row0.timestamp.end - row0.timestamp.start).total_seconds()
    assert row0_duration < THRESHOLD

    # 2. Row 1 Assertions (Should be over threshold but un-splittable) 📈⚠️
    assert row1.text.startswith("Ich bin dein Gastgeber")
    assert row1.timestamp.start == timedelta(milliseconds=FIRST_PERIOD_END_MS)
    assert row1.timestamp.end == timedelta(milliseconds=LAST_WORD_END_MS)

    row1_duration = (row1.timestamp.end - row1.timestamp.start).total_seconds()
    assert row1_duration > THRESHOLD  # 5.71s > 3.0s

    # 3. Warning Assertions 🛑👂
    # We expect a warning for row 1 because it's too long but has no more periods!
    assert "Utterance too long" in caplog.text
    assert "no terminal punctuation found to split at" in caplog.text
    assert "Ich bin dein Gastgeber" in caplog.text


def test_segmentation_logic_threshold_too_high_no_splits(raw_utterances):
    """
    Verifies that no splits occur when the segment is within the threshold. 🛡️⚖️
    """
    enricher = SentenceSegmentationEnricher(max_duration_seconds=12.0)
    enriched = enricher.enrich(raw_utterances, LanguageTag("de"))

    assert len(enriched) == len(raw_utterances)
    for i, u in enumerate(enriched):
        assert u.text == raw_utterances[i].text
