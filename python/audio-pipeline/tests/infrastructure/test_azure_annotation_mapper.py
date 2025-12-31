import json
import pytest
from src.infrastructure.azure_inference_annotation_mapper import AzureInferenceAnnotationMapper
from src.domain.value_objects import LanguageTag

def test_mapper_extracts_model_name_correctly():
    """Verifies regex extraction of model name from Azure endpoints. 🕵️‍♀️🔬"""
    mapper = AzureInferenceAnnotationMapper()
    
    # Standard Azure pattern
    endpoint = "https://my-resource.openai.azure.com/openai/deployments/gpt-4o-mini/chat/completions?api-version=2024-01-01"
    assert mapper.extract_model_name(endpoint) == "gpt-4o-mini"
    
    # Fallback pattern
    assert mapper.extract_model_name("https://weird-url.com") == "model"

def test_mapper_prepares_payload_structure():
    """Verifies that the generated payload follows the Azure Inference API structure. 📦✨"""
    mapper = AzureInferenceAnnotationMapper()
    texts = ["Ich vertritt", "Hallo"]
    language = LanguageTag("de")
    model_name = "test-model"
    context = ["Previous line"]
    
    payload = mapper.prepare_payload(texts, language, model_name, context)
    
    assert payload["model"] == model_name
    assert payload["temperature"] == 0.1
    assert payload["response_format"]["type"] == "json_schema"
    
    # Check messages
    messages = payload["messages"]
    assert len(messages) == 2
    assert "tutor for de learners" in messages[0]["content"]
    
    # Check user content (JSON-in-JSON pattern)
    user_data = json.loads(messages[1]["content"])
    assert user_data["context_reference"] == "Previous line"
    assert len(user_data["items_to_annotate"]) == 2
    assert user_data["items_to_annotate"][0]["text"] == "Ich vertritt"

def test_mapper_parses_response_correctly():
    """Verifies parsing of AI response into notes, including the 'OK' sentinel. 🧼🚿🎯"""
    mapper = AzureInferenceAnnotationMapper()
    num_texts = 3
    
    # Raw API response mock
    mock_data = {
        "choices": [{
            "message": {
                "content": json.dumps({
                    "annotations": [
                        {"id": "0", "note": "Grammar error"},
                        {"id": "1", "note": "OK"}, # Sentinel! ⚓️
                        {"id": "2", "note": "Slang used"}
                    ]
                })
            }
        }]
    }
    
    results = mapper.parse_response(num_texts, mock_data)
    
    assert len(results) == 3
    assert results[0] == "Grammar error"
    assert results[1] is None # Sentinel 'OK' maps back to None! ✅
    assert results[2] == "Slang used"

def test_mapper_handles_missing_annotations_in_response():
    """Verifies that missing IDs in the response map to None. 🛡️⚖️"""
    mapper = AzureInferenceAnnotationMapper()
    mock_data = {
        "choices": [{
            "message": {
                "content": json.dumps({"annotations": []})
            }
        }]
    }
    
    results = mapper.parse_response(2, mock_data)
    assert results == [None, None]
