import subprocess
import os
from src.domain.interfaces import IAudioProcessor, ILogger
from src.infrastructure.logging import NullLogger
from src.domain.entities import AudioArtifact


class FFmpegAudioProcessor(IAudioProcessor):
    def __init__(self, work_dir: str = None, logger: ILogger = NullLogger()):
        self.work_dir = work_dir
        self.logger = logger

    def normalize(self, source_path: str) -> AudioArtifact:
        """
        Uses ffmpeg to normalize audio to 16kHz, mono, 16-bit PCM WAV.
        """
        filename = os.path.basename(source_path).rsplit(".", 1)[0] + "_normalized.wav"
        output_path = os.path.join(
            self.work_dir or os.path.dirname(source_path), filename
        )

        # Ensure work_dir exists
        if self.work_dir:
            os.makedirs(self.work_dir, exist_ok=True)

        command = [
            "ffmpeg",
            "-y",
            "-loglevel",
            "error",
            "-i",
            source_path,
            "-ar",
            "16000",
            "-ac",
            "1",
            "-c:a",
            "pcm_s16le",
            output_path,
        ]

        self.logger.debug(f"Running FFmpeg command: {' '.join(command)}")

        try:
            result = subprocess.run(command, capture_output=True, text=True)
            if result.returncode != 0:
                raise RuntimeError(f"FFmpeg failed! Error: {result.stderr}")
        except FileNotFoundError:
            raise RuntimeError(f"FFmpeg binary not found! Please install ffmpeg. 🚫🔨")

        return AudioArtifact(file_path=output_path, format="wav", sample_rate=16000)
