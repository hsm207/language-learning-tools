import pytest
import os
import subprocess
from src.infrastructure.audio import FFmpegAudioProcessor


def test_ffmpeg_processor_creates_work_dir(mocker, tmp_path):
    """Hits the os.makedirs branch for work_dir. 🏎️💨"""
    source = tmp_path / "test.mp3"
    source.write_text("dummy")

    work_dir = tmp_path / "custom_work"

    # Mock ffmpeg to avoid actual execution
    mocker.patch("subprocess.run", return_value=mocker.Mock(returncode=0))

    processor = FFmpegAudioProcessor(work_dir=str(work_dir))
    processor.normalize(str(source))

    assert work_dir.exists()


def test_ffmpeg_processor_failure_on_missing_binary(mocker, tmp_path):
    """Hits the FileNotFoundError branch when ffmpeg is missing. 🚫🔨"""
    source = tmp_path / "test.mp3"
    source.write_text("dummy")
    mocker.patch("subprocess.run", side_effect=FileNotFoundError)

    processor = FFmpegAudioProcessor()
    with pytest.raises(RuntimeError, match="FFmpeg binary not found"):
        processor.normalize(str(source))


def test_ffmpeg_processor_failure_on_binary_error(mocker, tmp_path):
    """Hits the non-zero return code branch. 🏎️🥊"""
    source = tmp_path / "test.mp3"
    source.write_text("dummy")
    mock_res = mocker.Mock(returncode=1, stderr="SOTA Error!")
    mocker.patch("subprocess.run", return_value=mock_res)

    processor = FFmpegAudioProcessor()
    with pytest.raises(RuntimeError, match="FFmpeg failed! Error: SOTA Error!"):
        processor.normalize(str(source))
