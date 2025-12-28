import pytest
from src.infrastructure.factory import PipelineComponentFactory
from src.infrastructure.logging import NullLogger
from src.infrastructure.transcription import WhisperTranscriber, AzureFastTranscriber
from src.infrastructure.diarization import PyannoteDiarizer, NullDiarizer
from src.application.enrichers.merging import TokenMergerEnricher


class MockArgs:
    def __init__(self, use_azure=False, max_duration=15.0, target_language="en", translation_context=3, translation_batch=1):
        self.use_azure = use_azure
        self.max_duration = max_duration
        self.target_language = target_language
        self.translation_context = translation_context
        self.translation_batch = translation_batch


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
    
    # Mock credentials to avoid ValueError! 🔑
    mocker.patch.dict("os.environ", {"AZURE_SPEECH_KEY": "fake", "AZURE_SPEECH_REGION": "eastus2"})
    
    _, transcriber, diarizer, _, enrichers = factory.build_components()
    
    assert isinstance(transcriber, AzureFastTranscriber)
    assert isinstance(diarizer, NullDiarizer)
    # Check if TokenMergerEnricher is ABSENT in the Azure stack! 🧼🚿
    assert not any(isinstance(e, TokenMergerEnricher) for e in enrichers)
