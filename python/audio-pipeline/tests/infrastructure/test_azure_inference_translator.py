import json
import os
import pytest
from dotenv import load_dotenv
from src.infrastructure.azure_inference_translation import AzureInferenceTranslator
from src.domain.value_objects import LanguageTag
from src.infrastructure.logging import StandardLogger


@pytest.fixture(scope="module")
def real_translator():
    """
    Provides an instance of AzureInferenceTranslator initialized with real cloud credentials.
    """
    load_dotenv()
    endpoint = os.environ.get("AZURE_AI_INFERENCE_ENDPOINT")
    api_key = os.environ.get("AZURE_AI_INFERENCE_KEY")

    if not endpoint or not api_key:
        pytest.skip(
            "Integration tests skipped: Missing AZURE_AI_INFERENCE credentials."
        )

    return AzureInferenceTranslator(
        endpoint=endpoint,
        api_key=api_key,
        logger=StandardLogger(name="IntegrationTest"),
    )


@pytest.fixture
def mock_translator():
    """
    Provides a mocked instance of AzureInferenceTranslator for unit testing internal logic.
    """
    return AzureInferenceTranslator(
        endpoint="https://fake.models.ai.azure.com/chat/completions?api-version=2024-05-01-preview",
        api_key="fake-key",
        logger=StandardLogger(name="MockTest"),
    )


def test_azure_inference_translator_real_success(real_translator):
    """Verifies standard translation with real Azure endpoint. 🌍💎"""
    texts = ["Hallo, wie geht es dir?", "Ich liebe sauberen Code."]
    results = real_translator.translate(
        texts=texts,
        target_lang=LanguageTag("en")
    )

    assert len(results) == 2
    assert "how are you" in results[0].lower()
    assert "clean code" in results[1].lower()


def test_azure_inference_translator_real_empty_input(real_translator):
    """Verifies handling of empty input. 🧼🚿"""
    results = real_translator.translate([], LanguageTag("en"))
    assert results == []


def test_azure_inference_translator_real_context_fidelity(real_translator):
    """Verifies that context is used to disambiguate terms. 🎯🔬"""
    results = real_translator.translate(
        texts=["Ich gehe zur Bank."],
        target_lang=LanguageTag("en"),
        context=["Ich muss Geld abheben."],
    )

    assert any("bank" in r.lower() and "bench" not in r.lower() for r in results)


def test_azure_inference_translator_retry_on_429(mock_translator, mocker):
    """Verifies exponential backoff on 429. 🚦🔄"""
    mocker.patch("time.sleep")

    mock_response_429 = mocker.Mock()
    mock_response_429.status_code = 429

    mock_response_200 = mocker.Mock()
    mock_response_200.status_code = 200
    mock_response_200.json.return_value = {
        "choices": [
            {
                "message": {
                    "content": json.dumps(
                        {"translations": [{"id": "0", "text": "Hello"}]}
                    )
                }
            }
        ]
    }

    mock_post = mocker.patch(
        "httpx.Client.post", side_effect=[mock_response_429, mock_response_200]
    )

    results = mock_translator.translate(["Hallo"], LanguageTag("en"))

    assert results == ["Hello"]
    assert mock_post.call_count == 2


def test_azure_inference_translator_exhausts_retries_on_exception(
    mock_translator, mocker
):
    """Verifies default response on persistent failure. 😱💥"""
    mocker.patch("time.sleep")
    mocker.patch("httpx.Client.post", side_effect=Exception("Connection failure"))

    results = mock_translator.translate(
        ["Hallo", "Welt"], LanguageTag("en")
    )

    assert results == ["", ""]