import pytest
from datetime import timedelta
from src.application.enrichers.translation import TranslationEnricher
from src.domain.interfaces import ITranslator, ILogger
from src.domain.value_objects import (
    Utterance,
    TimestampRange,
    ConfidenceScore,
    LanguageTag,
)


def test_translation_enricher_sliding_window_logic(mocker):
    """
    Verifies that the TranslationEnricher correctly orchestrates
    a sliding window of context for the translator. 🏘️🧪⚓️🎯
    """
    # Arrange
    # 1. Mock the translator with the NEW expected signature! 📐
    mock_translator = mocker.Mock(spec=ITranslator)
    # We return a list of translated strings matching the input batch size
    mock_translator.translate.side_effect = lambda texts, sl, tl, context=None: [
        f"T_{t}" for t in texts
    ]

    # 2. Setup the Enricher with parameterized window sizes! 📏
    # Context Size = 2, Batch Size = 3
    enricher = TranslationEnricher(
        translator=mock_translator,
        target_lang=LanguageTag("en"),
        batch_size=3,
        context_size=2,
    )

    # 3. Create a sequence of 10 dummy utterances
    utterances = [
        Utterance(
            TimestampRange(timedelta(seconds=i), timedelta(seconds=i + 1)),
            f"Line_{i}",
            "SPK1",
            ConfidenceScore(1.0),
        )
        for i in range(10)
    ]

    # Act
    enriched = enricher.enrich(utterances, LanguageTag("de"))

    # Assert: Verification of the Orchestration Policy! 🏛️⚖️
    assert len(enriched) == 10
    assert mock_translator.translate.call_count == 4  # (3+3+3+1 = 10 lines)

    # Check Call 1 (Cold Start): Lines 0,1,2 | Context: None or Empty
    args, kwargs = mock_translator.translate.call_args_list[0]
    assert args[0] == ["Line_0", "Line_1", "Line_2"]
    assert kwargs.get("context") == [] or kwargs.get("context") is None

    # Check Call 2 (Steady State): Lines 3,4,5 | Context: [Line_1, Line_2]
    args, kwargs = mock_translator.translate.call_args_list[1]
    assert args[0] == ["Line_3", "Line_4", "Line_5"]
    assert kwargs.get("context") == ["Line_1", "Line_2"], "Window Math Error in Call 2!"

    # Check Call 3: Lines 6,7,8 | Context: [Line_4, Line_5]
    args, kwargs = mock_translator.translate.call_args_list[2]
    assert args[0] == ["Line_6", "Line_7", "Line_8"]
    assert kwargs.get("context") == ["Line_4", "Line_5"]

    # Check Call 4 (Tail): Line 9 | Context: [Line_7, Line_8]
    args, kwargs = mock_translator.translate.call_args_list[3]
    assert args[0] == ["Line_9"]
    assert kwargs.get("context") == ["Line_7", "Line_8"]
