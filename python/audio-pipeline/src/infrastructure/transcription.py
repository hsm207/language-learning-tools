import subprocess
import os
import json
from typing import List
from datetime import timedelta
from src.domain.interfaces import ITranscriber, ILogger
from src.infrastructure.logging import NullLogger
from src.domain.entities import AudioArtifact
from src.domain.value_objects import (
    Utterance,
    LanguageTag,
    TimestampRange,
    ConfidenceScore,
    Word,
)


class WhisperTranscriber(ITranscriber):
    def __init__(
        self, executable_path: str, model_path: str, logger: ILogger = NullLogger()
    ):
        self.executable_path = executable_path
        self.model_path = model_path
        self.logger = logger

    def transcribe(
        self, audio: AudioArtifact, language: LanguageTag
    ) -> List[Utterance]:
        """
        Runs whisper-cli and returns segments containing RAW tokens as words. 🎤🧩
        Precision starts here! Merging into words happens later in the pipeline. 🧼⚖️
        """
        output_base = audio.file_path.rsplit(".", 1)[0]
        command = [
            self.executable_path,
            "-m",
            self.model_path,
            "-f",
            audio.file_path,
            "-l",
            str(language),
            "-ojf",
            "-of",
            output_base,
            "-t",
            "8",
            "-sow",
        ]

        self.logger.debug(f"Running Whisper command: {' '.join(command)}")

        result = subprocess.run(command, capture_output=True, text=True)
        if result.returncode != 0:
            raise RuntimeError(f"Whisper failed! Error: {result.stderr}")

        json_path = f"{output_base}.json"
        if not os.path.exists(json_path):
            self.logger.error(f"❌ Whisper JSON output not found at {json_path}!")
            return []

        with open(json_path, "r", encoding="utf-8") as f:
            data = json.load(f)

        if not data or "transcription" not in data:
            return []

        utterances = []
        for segment in data.get("transcription", []):
            offsets = segment.get("offsets", {})
            seg_start = offsets.get("from", 0)
            seg_end = offsets.get("to", 0)

            # Step 1: Collect ALL raw tokens from this segment.
            # We treat tokens as 'Words' for now so the pipeline can process them.
            words = []
            for token in segment.get("tokens", []):
                t_text = token.get("text", "")

                # Filter out Whisper control tokens
                if not t_text or t_text.strip().startswith("[_"):
                    continue

                t_offsets = token.get("offsets", {})
                t_start = t_offsets.get("from", seg_start)
                t_end = t_offsets.get("to", seg_end)
                t_conf = token.get("p", 1.0)

                words.append(
                    Word(
                        text=t_text,  # KEEP RAW TEXT (including spaces) for later merging logic!
                        timestamp=TimestampRange(
                            start=timedelta(milliseconds=t_start),
                            end=timedelta(milliseconds=t_end),
                        ),
                        confidence=ConfidenceScore(t_conf),
                    )
                )

            if not words:
                continue

            # Return the segment as an Utterance bounded by its tokens! 📏🎯
            utterances.append(
                Utterance(
                    timestamp=TimestampRange(
                        start=words[0].timestamp.start, end=words[-1].timestamp.end
                    ),
                    text=segment.get("text", "").strip(),
                    speaker_id="Unknown",
                    confidence=ConfidenceScore(1.0),
                    words=words,
                )
            )

        return utterances
