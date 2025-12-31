from dataclasses import dataclass
import pytest
from src.infrastructure.factory import PipelineComponentFactory
from src.infrastructure.logging import NullLogger
from src.infrastructure.transcription import WhisperTranscriber, AzureFastTranscriber
from src.infrastructure.diarization import PyannoteDiarizer, NullDiarizer
from src.application.enrichers.merging import TokenMergerEnricher


@dataclass
class MockArgs:
    input: str = "test.mp3"
    output_dir: str = "./output"
    language: str = "de-DE"
    target_language: str = "en"
    num_speakers: int = 2
    max_duration: float = 15.0
    translation_context: int = 3
    translation_batch: int = 10
    annotation_context: int = 10
    annotation_batch: int = 1
    use_azure: bool = False


def test_factory_builds_local_stack(mocker):
    """Verifies factory builds the local Whisper/Pyannote stack when use_azure is False. 🏠🛡️"""
    mocker.patch("os.path.exists", return_value=True)
    # Mock HF_TOKEN to satisfy Pyannote! 🏷️✨
    mocker.patch.dict("os.environ", {"HF_TOKEN": "fake_token"})

    args = MockArgs(use_azure=False)
    logger = NullLogger()
    factory = PipelineComponentFactory(args, logger)

    _, transcriber, diarizer, _, enrichers = factory.build_components()

    assert isinstance(transcriber, WhisperTranscriber)
    assert isinstance(diarizer, PyannoteDiarizer)
    # Check if TokenMergerEnricher is present in the local stack! 🧩
    assert any(isinstance(e, TokenMergerEnricher) for e in enrichers)


def test_factory_builds_azure_stack(mocker):
    """Verifies factory builds the Azure Fast Transcription stack when use_azure is True. ☁️🏎️💨"""
    args = MockArgs(use_azure=True)
    logger = NullLogger()
    factory = PipelineComponentFactory(args, logger)

    # Mock ALL mandatory credentials! 🔑
    mocker.patch.dict(
        "os.environ",
        {
            "AZURE_SPEECH_KEY": "fake",
            "AZURE_SPEECH_REGION": "eastus2",
            "AZURE_AI_INFERENCE_KEY": "fake",
            "AZURE_AI_INFERENCE_ENDPOINT": "https://fake.models.ai.azure.com/chat/completions?api-version=2024-05-01-preview",
        },
    )

    _, transcriber, diarizer, _, enrichers = factory.build_components()

    assert isinstance(transcriber, AzureFastTranscriber)
    assert isinstance(diarizer, NullDiarizer)
    # Check if TokenMergerEnricher is ABSENT in the Azure stack! 🧼🚿
    assert not any(isinstance(e, TokenMergerEnricher) for e in enrichers)
