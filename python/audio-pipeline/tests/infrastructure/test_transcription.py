import pytest
import subprocess
from src.domain.value_objects import LanguageTag
from src.domain.entities import AudioArtifact
from src.infrastructure.transcription import WhisperTranscriber, AzureFastTranscriber


def test_whisper_transcriber_failure_on_missing_binary(mocker):
    """Hits the FileNotFoundError branch when whisper is missing. 🚫🔨"""
    mocker.patch("subprocess.run", side_effect=FileNotFoundError)

    transcriber = WhisperTranscriber("invalid_path", "model_path")
    with pytest.raises(RuntimeError, match="Whisper binary not found"):
        transcriber.transcribe(AudioArtifact(file_path="test.wav"), LanguageTag("de"))


def test_azure_fast_transcriber_success(mocker):
    """Verifies AzureFastTranscriber maps the Azure JSON correctly! ☁️🏷️✨"""
    mock_response = mocker.Mock()
    mock_response.status_code = 200
    mock_response.json.return_value = {
        "phrases": [
            {
                "offsetMilliseconds": 1000,
                "durationMilliseconds": 2000,
                "text": "Hello world",
                "speaker": 1,
                "confidence": 0.95,
                "words": [
                    {
                        "text": "Hello",
                        "offsetMilliseconds": 1000,
                        "durationMilliseconds": 500,
                        "confidence": 0.99,
                    },
                    {
                        "text": "world",
                        "offsetMilliseconds": 1500,
                        "durationMilliseconds": 500,
                        "confidence": 0.91,
                    },
                ],
            }
        ]
    }

    # Mock httpx.Client and the post method! 🏎️💨
    mock_client = mocker.MagicMock()
    mock_client.__enter__.return_value = mock_client
    mock_client.post.return_value = mock_response
    mocker.patch("httpx.Client", return_value=mock_client)

    # Mock open to avoid FileNotFoundError! 📁✨
    mocker.patch("builtins.open", mocker.mock_open(read_data=b"fake_audio_content"))

    transcriber = AzureFastTranscriber(api_key="fake_key", region="eastus2")
    results = transcriber.transcribe(
        AudioArtifact(file_path="test.wav"), LanguageTag("en")
    )

    assert len(results) == 1
    utterance = results[0]
    assert utterance.text == "Hello world"
    assert utterance.speaker_id == "1"
    assert utterance.timestamp.start.total_seconds() == 1.0
    assert utterance.timestamp.end.total_seconds() == 3.0
    assert len(utterance.words) == 2
    assert utterance.words[0].text == "Hello"
    assert utterance.words[0].timestamp.start.total_seconds() == 1.0
    assert utterance.words[1].text == "world"
    assert utterance.words[1].timestamp.end.total_seconds() == 2.0


def test_azure_fast_transcriber_failure(mocker):
    """Verifies AzureFastTranscriber raises RuntimeError on API failure. ❌☁️"""
    mock_response = mocker.Mock()
    mock_response.status_code = 401
    mock_response.text = "Unauthorized"

    mock_client = mocker.MagicMock()
    mock_client.__enter__.return_value = mock_client
    mock_client.post.return_value = mock_response
    mocker.patch("httpx.Client", return_value=mock_client)
    mocker.patch("builtins.open", mocker.mock_open(read_data=b"fake_audio_content"))

    transcriber = AzureFastTranscriber(api_key="fake_key", region="eastus2")
    with pytest.raises(
        RuntimeError, match="Azure Fast Transcription failed! Status: 401"
    ):
        transcriber.transcribe(AudioArtifact(file_path="test.wav"), LanguageTag("en"))


def test_azure_fast_transcriber_initialization(mocker):
    """Verifies AzureFastTranscriber initializes with provided keys. ☁️🗝️✨"""
    transcriber = AzureFastTranscriber(api_key="fake_key", region="eastus2")
    assert transcriber.api_key == "fake_key"
    assert "eastus2" in transcriber.endpoint
