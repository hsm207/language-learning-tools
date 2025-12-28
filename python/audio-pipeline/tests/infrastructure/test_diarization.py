import os
import pytest
from unittest.mock import Mock, MagicMock
from src.infrastructure.diarization import PyannoteDiarizer
from src.domain.entities import AudioArtifact
from src.domain.value_objects import DiarizationOptions


def test_pyannote_diarizer_fails_on_missing_token(mocker):
    """Hits the branch where HF_TOKEN is not in environment. 🔐🚫"""
    mocker.patch.dict(os.environ, {"HF_TOKEN": ""})
    with pytest.raises(ValueError, match="Missing HF_TOKEN"):
        PyannoteDiarizer()


def test_pyannote_diarizer_fails_on_pipeline_load_error(mocker):
    """Hits the exception branch during pipeline initialization. 😱🥊"""
    mocker.patch.dict(os.environ, {"HF_TOKEN": "valid"})
    mocker.patch(
        "pyannote.audio.Pipeline.from_pretrained",
        side_effect=Exception("Network Error"),
    )

    with pytest.raises(Exception, match="Network Error"):
        PyannoteDiarizer()


def test_pyannote_diarizer_fails_on_empty_pipeline(mocker):
    """Hits the None check if from_pretrained returns nothing. 👻🔍"""
    mocker.patch.dict(os.environ, {"HF_TOKEN": "valid"})
    mocker.patch("pyannote.audio.Pipeline.from_pretrained", return_value=None)

    with pytest.raises(RuntimeError, match="pipeline failed to load"):
        PyannoteDiarizer()


def test_pyannote_diarizer_guards_against_uninitialized_call(mocker):
    """Hits the defensive guard in the diarize method. 🛡️⚖️"""
    mocker.patch.dict(os.environ, {"HF_TOKEN": "valid"})
    mocker.patch("pyannote.audio.Pipeline.from_pretrained", return_value=Mock())

    diarizer = PyannoteDiarizer()
    diarizer.pipeline = None  # Force broken state!

    with pytest.raises(RuntimeError, match="not initialized"):
        diarizer.diarize(AudioArtifact(file_path="test.wav"))


def test_pyannote_diarize_with_full_options(mocker):
    """Hits all the option-flattening branches in the diarize method. 🏎️💨"""
    mocker.patch.dict(os.environ, {"HF_TOKEN": "valid"})
    mock_pipeline = MagicMock()
    mocker.patch("pyannote.audio.Pipeline.from_pretrained", return_value=mock_pipeline)

    diarizer = PyannoteDiarizer()
    options = DiarizationOptions(num_speakers=2, min_speakers=1, max_speakers=3)

    diarizer.diarize(AudioArtifact(file_path="test.wav"), options=options)

    # Verify the flattened kwargs passed to the pipeline! 🎯
    _, kwargs = mock_pipeline.call_args
    assert kwargs["num_speakers"] == 2
    assert kwargs["min_speakers"] == 1
    assert kwargs["max_speakers"] == 3
