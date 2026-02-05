import subprocess
import json
import os
import httpx
from typing import List
from src.domain.interfaces import ITranscriber, ILogger
from src.infrastructure.logging import NullLogger
from src.domain.entities import AudioArtifact
from src.domain.value_objects import (
    Utterance,
    LanguageTag,
)
from src.infrastructure.mappers.transcription import WhisperOutputMapper, AzureTranscriptionMapper


class WhisperTranscriber(ITranscriber):
    def __init__(
        self, executable_path: str, model_path: str, logger: ILogger = NullLogger()
    ):
        self.executable_path = executable_path
        self.model_path = model_path
        self.logger = logger
        self.mapper = WhisperOutputMapper()

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

        self.logger.debug(f"🚀 Spawning Whisper binary for local transcription...")

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

        return self.mapper.map(data)


class AzureFastTranscriber(ITranscriber):
    """
    Azure AI Speech Fast Transcription implementation. 🎤☁️✨
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
        self.mapper = AzureTranscriptionMapper()

    def transcribe(
        self, audio: AudioArtifact, language: LanguageTag
    ) -> List[Utterance]:
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

        # 🏛️ Enshrine the raw response as an intermediary artifact for forensic analysis! 💎✨
        raw_output_path = audio.file_path.rsplit(".", 1)[0] + ".azure.json"
        try:
            with open(raw_output_path, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=2, ensure_ascii=False)
            self.logger.info(
                f"💾 Saved raw Azure transcription artifact to {raw_output_path} 🕵️‍♀️🔬"
            )
        except Exception as e:
            self.logger.warning(f"⚠️ Failed to save raw Azure artifact: {e}")

        return self.mapper.map(data)
