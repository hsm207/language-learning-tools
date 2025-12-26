from unittest.mock import Mock, MagicMock
from uuid import UUID
from src.application.pipeline import AudioProcessingPipeline
from src.domain.entities import AudioArtifact, JobStatus
from src.domain.interfaces import ITranscriber, IDiarizer, IAudioProcessor, ILogger
from src.application.services import AlignmentService

def test_pipeline_execution_flow():
    # 1. Setup Mocks
    processor = Mock(spec=IAudioProcessor)
    transcriber = Mock(spec=ITranscriber)
    diarizer = Mock(spec=IDiarizer)
    alignment = Mock(spec=AlignmentService)
    logger = Mock(spec=ILogger)
    
    # Configure mock behavior
    artifact = AudioArtifact(file_path="test.wav")
    processor.normalize.return_value = artifact
    transcriber.transcribe.return_value = []
    diarizer.diarize.return_value = []
    alignment.align.return_value = []
    
    pipeline = AudioProcessingPipeline(
        audio_processor=processor,
        transcriber=transcriber,
        diarizer=diarizer,
        alignment_service=alignment,
        logger=logger
    )
    
    # 2. Execute
    job = pipeline.execute("source.m4a", "de")
    
    # 3. Assert
    assert job.status == JobStatus.COMPLETED
    processor.normalize.assert_called_once_with("source.m4a")
    transcriber.transcribe.assert_called_once()
    diarizer.diarize.assert_called_once()
    alignment.align.assert_called_once()

def test_pipeline_failure_handles_exceptions():
    processor = Mock(spec=IAudioProcessor)
    processor.normalize.side_effect = Exception("Boom! 💥")
    
    pipeline = AudioProcessingPipeline(
        audio_processor=processor,
        transcriber=Mock(),
        diarizer=Mock(),
        alignment_service=Mock(),
        logger=Mock()
    )
    
    job = pipeline.execute("source.m4a", "de")
    
    assert job.status == JobStatus.FAILED
    assert "Boom! 💥" in job.error_message