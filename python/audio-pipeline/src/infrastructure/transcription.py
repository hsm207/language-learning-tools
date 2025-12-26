import subprocess
import os
import json
from typing import List
from datetime import timedelta
from src.domain.interfaces import ITranscriber, ILogger, NullLogger
from src.domain.entities import AudioArtifact
from src.domain.value_objects import Utterance, LanguageTag, TimestampRange, ConfidenceScore

class WhisperTranscriber(ITranscriber):
    def __init__(self, executable_path: str, model_path: str, logger: ILogger = NullLogger()):
        self.executable_path = executable_path
        self.model_path = model_path
        self.logger = logger

    def transcribe(self, audio: AudioArtifact, language: LanguageTag) -> List[Utterance]:
        """
        Runs whisper-cli and parses the JSON output for maximum precision! 🎯
        """
        output_base = audio.file_path.rsplit(".", 1)[0]
        command = [
            self.executable_path,
            "-m", self.model_path,
            "-f", audio.file_path,
            "-l", str(language),
            "-oj", # Output JSON for sexy metadata
            "-of", output_base,
            "-t", "8" # Using 8 threads for speed! 💨
        ]
        
        self.logger.debug(f"Running Whisper command: {' '.join(command)}")
            
        result = subprocess.run(command, capture_output=True, text=True)
        if result.returncode != 0:
            raise RuntimeError(f"Whisper failed, sugar! Error: {result.stderr}")
            
        json_path = f"{output_base}.json"
        self.logger.debug(f"Parsing Whisper output from {json_path}")
            
        with open(json_path, "r", encoding="utf-8") as f:
            data = json.load(f)
            
        utterances = []
        # whisper.cpp JSON structure uses 'transcription' key
        for segment in data.get("transcription", []):
            start_ms = segment.get("offsets", {}).get("from", 0)
            end_ms = segment.get("offsets", {}).get("to", 0)
            text = segment.get("text", "").strip()
            
            utterances.append(Utterance(
                timestamp=TimestampRange(
                    start=timedelta(milliseconds=start_ms),
                    end=timedelta(milliseconds=end_ms)
                ),
                text=text,
                speaker_id="Unknown", # To be filled by Diarizer/Alignment
                confidence=ConfidenceScore(1.0) # Placeholder
            ))
            
        return utterances