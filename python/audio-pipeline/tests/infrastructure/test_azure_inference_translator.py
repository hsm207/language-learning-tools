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

    Skips the test if required environment variables are missing. Validates that the
    endpoint follows the expected format for the Azure AI Inference API.
    """
    load_dotenv()
    endpoint = os.environ.get("AZURE_AI_INFERENCE_ENDPOINT")
    api_key = os.environ.get("AZURE_AI_INFERENCE_KEY")

    if not endpoint or not api_key:
        pytest.skip(
            "Integration tests skipped: Missing AZURE_AI_INFERENCE credentials."
        )

    if "/chat/completions" not in endpoint or "api-version=" not in endpoint:
        raise ValueError(
            f"Invalid endpoint format: '{endpoint}'. "
            "Expected a full URL including '/chat/completions' and API version parameters."
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
    """
    Verifies that the translator correctly handles a standard translation request
    using the real Azure AI Inference endpoint.
    """
    texts = ["Hallo, wie geht es dir?", "Ich liebe sauberen Code."]
    context = ["Technical discussion context."]

    results = real_translator.translate(
        texts=texts,
        source_lang=LanguageTag("de"),
        target_lang=LanguageTag("en"),
        context=context,
    )

    assert len(results) == 2
    assert "how are you" in results[0].lower()
    assert "clean code" in results[1].lower()


def test_azure_inference_translator_real_empty_input(real_translator):
    """
    Verifies that the translator returns an empty list when provided with empty input.
    """
    results = real_translator.translate([], LanguageTag("de"), LanguageTag("en"))
    assert results == []


def test_azure_inference_translator_real_context_fidelity(real_translator):
    """
    Verifies that the translator uses provided context to correctly disambiguate
    polysemous terms (e.g., 'Bank' as a financial institution vs. a bench).
    """
    results = real_translator.translate(
        texts=["Ich gehe zur Bank."],
        source_lang=LanguageTag("de"),
        target_lang=LanguageTag("en"),
        context=["Ich muss Geld abheben.", "Mein Konto ist voll."],
    )

    assert any("bank" in r.lower() and "bench" not in r.lower() for r in results)


def test_azure_inference_translator_retry_on_429(mock_translator, mocker):
    """
    Verifies that the translator implements exponential backoff and retries
    when encountering HTTP 429 (Too Many Requests) responses.
    """
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

    results = mock_translator.translate(["Hallo"], LanguageTag("de"), LanguageTag("en"))

    assert results == ["Hello"]
    assert mock_post.call_count == 2


def test_azure_inference_translator_handles_empty_choices(mock_translator, mocker):
    """
    Verifies that the translator gracefully handles scenarios where the AI model
    returns a successful response but with an empty list of choices.
    """
    mock_response = mocker.Mock()
    mock_response.status_code = 200
    mock_response.json.return_value = {"choices": []}
    mock_response.text = "Empty choices payload"

    mocker.patch("httpx.Client.post", return_value=mock_response)

    results = mock_translator.translate(["Hallo"], LanguageTag("de"), LanguageTag("en"))

    assert results == [""]
    assert len(results) == 1


def test_azure_inference_translator_exhausts_retries_on_exception(
    mock_translator, mocker
):
    """
    Verifies that the translator correctly handles persistent exceptions by
    returning a default response after exhausting the maximum number of retries.
    """
    mocker.patch("time.sleep")
    mocker.patch("httpx.Client.post", side_effect=Exception("Connection failure"))

    results = mock_translator.translate(
        ["Hallo", "Welt"], LanguageTag("de"), LanguageTag("en")
    )

    assert results == ["", ""]


def test_azure_inference_translator_exhausts_429_retries(mock_translator, mocker):
    """
    Verifies that the translator returns a default response when the maximum
    number of retries is exceeded due to persistent HTTP 429 responses.
    """
    mocker.patch("time.sleep")

    mock_response_429 = mocker.Mock()
    mock_response_429.status_code = 429

    mock_post = mocker.patch("httpx.Client.post", return_value=mock_response_429)

    results = mock_translator.translate(["Hallo"], LanguageTag("de"), LanguageTag("en"))

    assert results == [""]
    assert mock_post.call_count == 3
