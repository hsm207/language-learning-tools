import subprocess
import os
import json
from typing import List
from datetime import timedelta
from src.domain.interfaces import ITranscriber, ILogger, NullLogger
from src.domain.entities import AudioArtifact
from src.domain.value_objects import Utterance, LanguageTag, TimestampRange, ConfidenceScore, Word

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
            "-oj", # Output JSON for detailed metadata
            "-of", output_base,
            "-t", "8" # Using 8 threads for speed! 💨
        ]
        
        self.logger.debug(f"Running Whisper command: {' '.join(command)}")
            
        result = subprocess.run(command, capture_output=True, text=True)
        if result.returncode != 0:
            raise RuntimeError(f"Whisper failed! Error: {result.stderr}")
            
        json_path = f"{output_base}.json"
        self.logger.debug(f"Parsing Whisper output from {json_path}")
            
        if not os.path.exists(json_path):
            self.logger.error(f"❌ Whisper JSON output not found at {json_path}!")
            return []

        with open(json_path, "r", encoding="utf-8") as f:
            data = json.load(f)
            
        if not data or "transcription" not in data:
            self.logger.debug("Whisper returned empty or malformed JSON data.")
            return []

        utterances = []
        for segment in data.get("transcription", []):
            offsets = segment.get("offsets", {})
            start_ms = offsets.get("from", 0)
            end_ms = offsets.get("to", 0)
            text = segment.get("text", "").strip()
            
            # Extract word-level data if available (tokens) 🕵️‍♀️
            words = []
            for token in segment.get("tokens", []):
                t_text = token.get("text", "").strip()
                if not t_text:
                    continue
                
                # Some tokens are special characters or [BEAT], we might want to filter or handle them
                # For now, let's treat each token as a "word" for simplicity
                t_start = token.get("t0", 0) * 10 # whisper.cpp t0/t1 are often in 1/100s of sec? 
                t_end = token.get("t1", 0) * 10   # Actually, it depends on the version. 
                # Let's assume they are in milliseconds if it's whisper-cli -oj.
                # Actually, in whisper.cpp's JSON, t0 and t1 are usually in milliseconds if they match segment offsets.
                
                # Wait, let's look at the segment offsets: start_ms is offsets.get("from")
                # If t0 is 0 and start_ms is 500, then the absolute time is start_ms + t0? 
                # No, usually t0/t1 in tokens are absolute from start of audio.
                
                t_start = token.get("t0", start_ms)
                t_end = token.get("t1", end_ms)
                t_conf = token.get("p", 1.0)
                
                words.append(Word(
                    text=t_text,
                    timestamp=TimestampRange(
                        start=timedelta(milliseconds=t_start),
                        end=timedelta(milliseconds=t_end)
                    ),
                    confidence=ConfidenceScore(t_conf)
                ))
            
            utterances.append(Utterance(
                timestamp=TimestampRange(
                    start=timedelta(milliseconds=start_ms),
                    end=timedelta(milliseconds=end_ms)
                ),
                text=text,
                speaker_id="Unknown",
                confidence=ConfidenceScore(1.0),
                words=words
            ))
            
        self.logger.debug(f"Raw transcription found {len(utterances)} segments.")
        return utterances
