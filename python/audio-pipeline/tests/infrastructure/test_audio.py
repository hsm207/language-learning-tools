import pytest
import os
import subprocess
from unittest.mock import Mock
from src.infrastructure.audio import FFmpegAudioProcessor


def test_ffmpeg_processor_failure_on_missing_binary(mocker):
    """Hits the FileNotFoundError branch when ffmpeg is missing. 🚫🔨"""
    mocker.patch("os.path.exists", return_value=True)
    mocker.patch("subprocess.run", side_effect=FileNotFoundError)
    
    processor = FFmpegAudioProcessor()
    with pytest.raises(RuntimeError, match="FFmpeg binary not found"):
        processor.normalize("test.mp3")


def test_ffmpeg_processor_failure_on_binary_error(mocker):
    """Hits the non-zero return code branch. 🏎️🥊"""
    mocker.patch("os.path.exists", return_value=True)
    mock_res = mocker.Mock(returncode=1, stderr="SOTA Error!")
    mocker.patch("subprocess.run", return_value=mock_res)
    
    processor = FFmpegAudioProcessor()
    with pytest.raises(RuntimeError, match="FFmpeg failed! Error: SOTA Error!"):
        processor.normalize("test.mp3")
