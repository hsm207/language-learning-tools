import pytest
from datetime import timedelta
from src.application.enrichers.merging import TokenMergerEnricher
from src.domain.value_objects import Utterance, TimestampRange, ConfidenceScore, LanguageTag, Word


def test_token_merger_handles_utterance_without_words():
    """
    Behavioral Test: Verifies that the merger safely passes through
    utterances that have no words/tokens. 🧼💎⚖️
    """
    # Arrange
    enricher = TokenMergerEnricher()
    empty_u = Utterance(
        timestamp=TimestampRange(timedelta(0), timedelta(1)),
        text="No words here!",
        speaker_id="S1",
        confidence=ConfidenceScore(1.0),
        words=[], # THE MISSING BRANCH! 🎯
    )
    
    # Act
    results = enricher.enrich([empty_u], LanguageTag("de"))
    
    # Assert
    assert len(results) == 1
    assert results[0].words == []
    assert results[0].text == "No words here!"


def test_token_merger_reconstructs_words_from_tokens():
    """
    Behavioral Test: Verifies that tokens without leading spaces
    are merged into the preceding word. 🎤🧩
    """
    # Arrange
    enricher = TokenMergerEnricher()
    words = [
        Word(" Hal", TimestampRange(timedelta(0), timedelta(0.5)), ConfidenceScore(0.9)),
        Word("lo", TimestampRange(timedelta(0.5), timedelta(1.0)), ConfidenceScore(0.9)),
    ]
    u = Utterance(TimestampRange(timedelta(0), timedelta(1)), "Hallo", "S1", ConfidenceScore(1.0), words=words)
    
    # Act
    results = enricher.enrich([u], LanguageTag("de"))
    
    # Assert
    assert results[0].text == "Hallo"
    assert len(results[0].words) == 1
    assert results[0].words[0].text == "Hallo"
