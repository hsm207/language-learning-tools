import os
import pytest
from dotenv import load_dotenv
from src.infrastructure.azure_inference_annotation import AzureInferenceAnnotationService
from src.domain.value_objects import LanguageTag
from src.infrastructure.logging import StandardLogger

@pytest.fixture
def annotation_service():
    load_dotenv()
    endpoint = os.environ.get("AZURE_AI_INFERENCE_ENDPOINT")
    api_key = os.environ.get("AZURE_AI_INFERENCE_KEY")
    
    if not endpoint or not api_key:
        pytest.skip("Azure Inference credentials not found! 🛡️⚖️")
        
    logger = StandardLogger(name="TestAnnotation")
    return AzureInferenceAnnotationService(endpoint=endpoint, api_key=api_key, logger=logger)

def test_azure_annotation_identifies_hall_of_shame_mistakes(annotation_service):
    """
    INTEGRATION TEST: Verifies that the hosted model can actually catch the 
    specific linguistic artifacts we found in the wild! 🕵️‍♀️🔬💎
    """
    # Arrange
    texts = [
        "Ich vertritt eine andere Haltung",           # 1. Conjugation Error
        "der der Bundes-CDU",                         # 2. Repetition/Stutter
        "D.",                                         # 3. Abbreviation Fragment
        "Wir arbeiten in Thüringen sehr konstruktiv zusammen." # 4. Perfect sentence
    ]
    
    # Act
    annotations = annotation_service.annotate(
        texts=texts,
        language=LanguageTag("de-DE")
    )
    
    # Assert
    assert len(annotations) == 4
    
    # 1. Check conjugation error note
    assert annotations[0] is not None
    assert "vertritt" in annotations[0].lower() or "conjugation" in annotations[0].lower()
    print(f"\n✅ Case 1 (Grammar): {annotations[0]}")
    
    # 2. Check repetition note
    assert annotations[1] is not None
    assert "repetition" in annotations[1].lower() or "stutter" in annotations[1].lower() or "der der" in annotations[1].lower()
    print(f"✅ Case 2 (Repetition): {annotations[1]}")
    
    # 3. Check fragment note
    assert annotations[2] is not None
    assert "fragment" in annotations[2].lower() or "abbreviation" in annotations[2].lower()
    print(f"✅ Case 3 (Fragment): {annotations[2]}")
    
    # 4. Check perfect sentence (should be None or null)
    # Note: LLMs sometimes give a note anyway, but our prompt asks for null.
    print(f"✅ Case 4 (Perfect): {annotations[3]}")
    assert annotations[3] is None or annotations[3] == ""
