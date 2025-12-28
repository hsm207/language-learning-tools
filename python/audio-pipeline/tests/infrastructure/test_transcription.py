import pytest
import subprocess
from src.domain.value_objects import LanguageTag
from src.domain.entities import AudioArtifact
from src.infrastructure.transcription import WhisperTranscriber


def test_whisper_transcriber_failure_on_missing_binary(mocker):
    """Hits the FileNotFoundError branch when whisper is missing. 🚫🔨"""
    mocker.patch("subprocess.run", side_effect=FileNotFoundError)

    transcriber = WhisperTranscriber("invalid_path", "model_path")
    with pytest.raises(RuntimeError, match="Whisper binary not found"):
        transcriber.transcribe(AudioArtifact(file_path="test.wav"), LanguageTag("de"))
