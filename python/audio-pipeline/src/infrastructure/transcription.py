import subprocess
import json
import os
import httpx
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

        try:
            result = subprocess.run(command, capture_output=True, text=True)
            if result.returncode != 0:
                raise RuntimeError(f"Whisper failed! Error: {result.stderr}")
        except FileNotFoundError:
            raise RuntimeError(
                f"Whisper binary not found at {self.executable_path}! 🚫🔨"
            )

        json_path = f"{output_base}.json"

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


class AzureFastTranscriber(ITranscriber):
    """
    SOTA Azure AI Speech Fast Transcription implementation. 🎤☁️✨
    Returns utterances with speaker labels already attached (Cloud-Native Diarization! 🏷️).
    """

    def __init__(
        self,
        api_key: str,
        region: str,
        logger: ILogger = NullLogger(),
    ):
        self.api_key = api_key
        self.region = region
        self.logger = logger
        self.endpoint = f"https://{self.region}.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2025-10-15"

    def transcribe(
        self, audio: AudioArtifact, language: LanguageTag
    ) -> List[Utterance]:
        self.logger.info(f"🚀 Launching Azure Fast Transcription for {audio.file_path}...")

        definition = {
            "locales": [str(language)],
            "diarization": {"enabled": True},
            "wordLevelTimestampsEnabled": True,
            "profanityFilterMode": "None",
            "model": "azure-speech",
        }

        with open(audio.file_path, "rb") as f:
            files = {
                "audio": (os.path.basename(audio.file_path), f, "audio/wav"),
                "definition": (None, json.dumps(definition), "application/json"),
            }

            headers = {"Ocp-Apim-Subscription-Key": self.api_key}

            with httpx.Client(timeout=300.0) as client:
                response = client.post(self.endpoint, headers=headers, files=files)

        if response.status_code != 200:
            error_msg = f"❌ Azure Fast Transcription failed! Status: {response.status_code}, Error: {response.text}"
            self.logger.error(error_msg)
            raise RuntimeError(error_msg)

        data = response.json()
        return self._map_to_utterances(data)

    def _map_to_utterances(self, data: dict) -> List[Utterance]:
        utterances = []
        for phrase in data.get("phrases", []):
            offset_ms = phrase.get("offsetMilliseconds", 0)
            duration_ms = phrase.get("durationMilliseconds", 0)
            speaker_id = str(phrase.get("speaker", "Unknown"))

            words = []
            for word_data in phrase["words"]:
                w_offset = word_data.get("offsetMilliseconds", offset_ms)
                w_duration = word_data.get("durationMilliseconds", 0)
                words.append(
                    Word(
                        text=word_data.get("text", ""),
                        timestamp=TimestampRange(
                            start=timedelta(milliseconds=w_offset),
                            end=timedelta(milliseconds=w_offset + w_duration),
                        ),
                        confidence=ConfidenceScore(word_data.get("confidence", 1.0)),
                    )
                )

            utterances.append(
                Utterance(
                    timestamp=TimestampRange(
                        start=timedelta(milliseconds=offset_ms),
                        end=timedelta(milliseconds=offset_ms + duration_ms),
                    ),
                    text=phrase.get("text", ""),
                    speaker_id=speaker_id,
                    confidence=ConfidenceScore(phrase.get("confidence", 1.0)),
                    words=words,
                )
            )

        self.logger.info(f"✅ Azure Transcription complete! Found {len(utterances)} phrases.")
        return utterances