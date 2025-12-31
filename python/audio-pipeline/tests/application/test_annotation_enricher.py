import pytest
import dataclasses
from datetime import timedelta
from typing import List, Optional
from src.application.enrichers.annotation import LinguisticAnnotationEnricher
from src.domain.interfaces import ILinguisticAnnotationService
from src.domain.value_objects import Utterance, LanguageTag, TimestampRange, ConfidenceScore
from src.infrastructure.logging import StandardLogger

class MockAnnotationService(ILinguisticAnnotationService) :
    def __init__(self):
        self.captured_contexts = []

    def annotate(
        self,
        texts: List[str],
        language: LanguageTag,
        context: Optional[List[str]] = None,
    ) -> List[Optional[str]]:
        self.captured_contexts.append(context)
        return [None] * len(texts)

def create_simple_utterance(text: str) -> Utterance:
    return Utterance(
        timestamp=TimestampRange(timedelta(0), timedelta(seconds=1)),
        text=text,
        speaker_id="S1",
        confidence=ConfidenceScore(1.0)
    )

def test_enricher_provides_bidirectional_context():
    """
    UNIT TEST: Verifies that the LinguisticAnnotationEnricher correctly slices
    both past and future utterances to provide a panoramic context to the service. 🔄🏔️💎
    """
    # Arrange
    utterances = [
        create_simple_utterance("Row 1"),
        create_simple_utterance("Row 2"),
        create_simple_utterance("Row 3"), # 🎯 Target
        create_simple_utterance("Row 4"),
        create_simple_utterance("Row 5"),
    ]
    
    mock_service = MockAnnotationService()
    # Batch size 1, Context size 2
    enricher = LinguisticAnnotationEnricher(
        annotation_service=mock_service,
        batch_size=1,
        context_size=2
    )
    
    # Act
    enricher.enrich(utterances, LanguageTag("de"))
    
    # Assert for Row 3 (Index 2)
    # The call for Row 3 is the 3rd call (index 2)
    row3_context = mock_service.captured_contexts[2]
    
    # Pre-context should be Row 1, Row 2
    # Post-context should be Row 4, Row 5
    assert "Row 1" in row3_context
    assert "Row 2" in row3_context
    assert "--- TARGET SEGMENT(S) BELOW ---" in row3_context
    assert "Row 4" in row3_context
    assert "Row 5" in row3_context
    
    # Verify bounds: It shouldn't include Row 3 in the context itself
    assert "Row 3" not in row3_context
    
    print("\n✅ Bidirectional context slicing verified! 🔄🏔️")

def test_enricher_handles_context_at_boundaries():
    """Verifies context slicing at the start and end of the transcript. 🛡️⚖️"""
    utterances = [
        create_simple_utterance("Start"),
        create_simple_utterance("Middle"),
        create_simple_utterance("End")
    ]
    mock_service = MockAnnotationService()
    enricher = LinguisticAnnotationEnricher(mock_service, batch_size=1, context_size=10)
    
    enricher.enrich(utterances, LanguageTag("de"))
    
    # 🏁 First Row Context (Start of Transcript)
    # Since there is no history, the 'Target Separator' must be the very first item.
    context_at_start = mock_service.captured_contexts[0]
    assert context_at_start[0] == "--- TARGET SEGMENT(S) BELOW ---"
    assert context_at_start[1] == "Middle"
    assert context_at_start[2] == "End"
    
    # 🔚 Last Row Context (End of Transcript)
    # Since there is no future, the 'Target Separator' must be the very last item.
    context_at_end = mock_service.captured_contexts[2]
    assert context_at_end[0] == "Start"
    assert context_at_end[1] == "Middle"
    assert context_at_end[2] == "--- TARGET SEGMENT(S) BELOW ---"

def test_enricher_is_resilient_to_service_failures(caplog):
    """
    UNIT TEST: Verifies that the enricher doesn't crash if the annotation service fails.
    It should mark the row with an explicit error sentinel! 🛡️⚖️💎✨
    """
    # Arrange
    u1 = create_simple_utterance("Stay Pure")
    
    class ExplodingService(ILinguisticAnnotationService):
        def annotate(self, *args, **kwargs):
            raise RuntimeError("AI MELTDOWN! 😱💥")
            
    enricher = LinguisticAnnotationEnricher(
        annotation_service=ExplodingService(),
        logger=StandardLogger(name="ResilienceTest")
    )
    
    # Act
    with caplog.at_level("ERROR"):
        results = enricher.enrich([u1], LanguageTag("de"))
    
    # Assert
    assert len(results) == 1
    assert results[0].text == "Stay Pure"
    # Now it is distinguishable! 🚩✨
    assert results[0].learner_notes == "[Annotation Service Unavailable ⚠️]"
    assert "Annotation failed" in caplog.text
    print("\n✅ Resilience verified: Error sentinel correctly set! 🛡️🌊")

def test_enricher_detects_contract_mismatch(caplog):
    """
    UNIT TEST: Verifies that the enricher detects when the service returns 
    the wrong number of annotations (Contract Violation). 🏛️⚖️🚫
    """
    # Arrange
    u1 = create_simple_utterance("Contract Test")
    
    class LyingService(ILinguisticAnnotationService):
        def annotate(self, *args, **kwargs):
            return ["Note 1", "Note 2"] # 👈 Returning 2 notes for 1 text! 😱
            
    enricher = LinguisticAnnotationEnricher(
        annotation_service=LyingService(),
        logger=StandardLogger(name="ContractTest")
    )
    
    # Act
    with caplog.at_level("ERROR"):
        enricher.enrich([u1], LanguageTag("de"))
        
    # Assert
    assert "Annotation count mismatch" in caplog.text
    print("\n✅ Contract Enforcement verified: Caught the lying service! 🛡️⚖️🏆")
