import json
import pytest
from src.infrastructure.azure_inference_translation_mapper import AzureInferenceTranslationMapper
from src.domain.value_objects import LanguageTag

def test_translation_mapper_extracts_model_name_correctly():
    """Verifies regex extraction of model name from Azure endpoints. 🕵️‍♀️🔬"""
    mapper = AzureInferenceTranslationMapper()
    endpoint = "https://test.openai.azure.com/openai/deployments/gpt-4o/chat/completions?api-version=2024-01-01"
    assert mapper.extract_model_name(endpoint) == "gpt-4o"

def test_translation_mapper_prepares_payload_structure():
    """Verifies that the generated payload follows the Azure Inference API structure. 📦✨"""
    mapper = AzureInferenceTranslationMapper()
    texts = ["Hallo", "Welt"]
    target_lang = LanguageTag("en")
    model_name = "translator-model"
    context = ["Context"]
    
    payload = mapper.prepare_payload(texts, target_lang, model_name, context)
    
    assert payload["model"] == model_name
    assert payload["temperature"] == 0.3
    
    # Check messages
    messages = payload["messages"]
    assert "Translate the provided list" in messages[0]["content"]
    
    # Check user content
    user_data = json.loads(messages[1]["content"])
    assert user_data["context_reference"] == "Context"
    assert len(user_data["items_to_translate"]) == 2

def test_translation_mapper_parses_response_correctly():
    """Verifies parsing of AI response into translated strings. 🧼🚿🎯"""
    mapper = AzureInferenceTranslationMapper()
    num_texts = 2
    
    mock_data = {
        "choices": [{
            "message": {
                "content": json.dumps({
                    "translations": [
                        {"id": "0", "text": "Hello"},
                        {"id": "1", "text": "World"}
                    ]
                })
            }
        }]
    }
    
    results = mapper.parse_response(num_texts, mock_data)
    assert results == ["Hello", "World"]

def test_translation_mapper_handles_missing_ids_gracefully():
    """Verifies that missing IDs in the response map to empty strings. 🛡️⚖️"""
    mapper = AzureInferenceTranslationMapper()
    mock_data = {
        "choices": [{
            "message": {
                "content": json.dumps({"translations": []})
            }
        }]
    }
    
    results = mapper.parse_response(2, mock_data)
    assert results == ["", ""]
