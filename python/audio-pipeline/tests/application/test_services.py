from datetime import timedelta
from src.domain.value_objects import Utterance, TimestampRange, ConfidenceScore
from src.application.services import MaxOverlapAlignmentService

def test_alignment_service_simple():
    # Arrange
    service = MaxOverlapAlignmentService()
    
    # Transcription: "Hello" at 1-2s
    transcription = [
        Utterance(
            timestamp=TimestampRange(timedelta(seconds=1), timedelta(seconds=2)),
            text="Hello",
            speaker_id="Unknown",
            confidence=ConfidenceScore(1.0)
        )
    ]
    
    # Diarization: Speaker A from 0-5s
    diarization = [
        Utterance(
            timestamp=TimestampRange(timedelta(seconds=0), timedelta(seconds=5)),
            text="",
            speaker_id="Speaker A",
            confidence=ConfidenceScore(1.0)
        )
    ]
    
    result = service.align(transcription, diarization)
    
    assert len(result) == 1
    assert result[0].text == "Hello"
    assert result[0].speaker_id == "Speaker A"

def test_align_no_overlap_defaults_to_unknown():
    service = MaxOverlapAlignmentService()
    
    transcription = [
        Utterance(
            timestamp=TimestampRange(timedelta(seconds=10), timedelta(seconds=11)),
            text="Ghost",
            speaker_id="Unknown",
            confidence=ConfidenceScore(1.0)
        )
    ]
    
    diarization = [
        Utterance(
            timestamp=TimestampRange(timedelta(seconds=0), timedelta(seconds=5)),
            text="",
            speaker_id="Speaker A",
            confidence=ConfidenceScore(1.0)
        )
    ]
    
    result = service.align(transcription, diarization)
    assert result[0].speaker_id == "Unknown"