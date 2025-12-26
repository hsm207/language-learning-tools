import subprocess
import os
from src.domain.interfaces import IAudioProcessor, ILogger, NullLogger
from src.domain.entities import AudioArtifact

class FFmpegAudioProcessor(IAudioProcessor):
    def __init__(self, logger: ILogger = NullLogger()):
        self.logger = logger

    def normalize(self, source_path: str) -> AudioArtifact:
        """
        Uses ffmpeg to normalize audio to 16kHz, mono, 16-bit PCM WAV.
        """
        if not os.path.exists(source_path):
            raise FileNotFoundError(f"Audio file not found at {source_path}! 😱")

        output_path = source_path.rsplit(".", 1)[0] + "_normalized.wav"
        
        command = [
            "ffmpeg", "-y", "-loglevel", "error", "-i", source_path,
            "-ar", "16000",
            "-ac", "1",
            "-c:a", "pcm_s16le",
            output_path
        ]
        
        self.logger.debug(f"Running FFmpeg command: {' '.join(command)}")
        
        result = subprocess.run(command, capture_output=True, text=True)
        if result.returncode != 0:
            raise RuntimeError(f"FFmpeg failed! Error: {result.stderr}")
            
        return AudioArtifact(
            file_path=output_path,
            format="wav",
            sample_rate=16000
        )
