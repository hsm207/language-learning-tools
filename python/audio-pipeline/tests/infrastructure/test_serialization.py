import json
from datetime import timedelta
from src.domain.value_objects import (
    AudioTranscript,
    Utterance,
    TimestampRange,
    ConfidenceScore,
    Word,
    LanguageTag,
)
from src.infrastructure.serialization import JsonTranscriptSerializer


def test_json_transcript_serializer_projects_all_fields():
    """
    Verifies that the Serializer adheres to our new
    Translation Contract by outputting all required schema fields! 🛡️⚖️🏛️
    """
    # Arrange
    words = [
        Word(
            text="Hallo",
            timestamp=TimestampRange(timedelta(0), timedelta(seconds=1)),
            confidence=ConfidenceScore(0.99),
        )
    ]
    utterance = Utterance(
        timestamp=TimestampRange(timedelta(0), timedelta(seconds=1)),
        text="Hallo",
        speaker_id="SPEAKER_01",
        confidence=ConfidenceScore(0.99),
        words=words,
        translated_text="Hello",  # This is our new requirement! 🗽💎
    )
    transcript = AudioTranscript(
        utterances=[utterance],
        target_language=LanguageTag("en"),  # This is our new requirement! 🗽💎
    )
    serializer = JsonTranscriptSerializer()

    # Act
    json_output = serializer.serialize(transcript)
    data = json.loads(json_output)

    # Assert: Verification of the Public API Contract! 🏛️⚖️
    # If any of these fail, we have a regression! 📉🥊
    assert (
        data["target_language"] == "en"
    ), "Root 'target_language' missing from API contract!"
    assert len(data["utterances"]) == 1

    u_data = data["utterances"][0]
    assert u_data["speaker"] == "SPEAKER_01"
    assert u_data["text"] == "Hallo"
    assert (
        u_data["translated_text"] == "Hello"
    ), "Utterance 'translated_text' missing from API contract!"
    assert "start" in u_data
    assert "end" in u_data
    assert "confidence" in u_data
    assert "words" in u_data
    assert len(u_data["words"]) == 1
