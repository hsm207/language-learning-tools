import pytest
from datetime import timedelta
from unittest.mock import Mock
from src.application.pipeline import AudioProcessingPipeline
from src.domain.interfaces import ITelemetryService
from src.domain.entities import ProcessingJob, JobStatus
from src.domain.events import PipelineStepTimed


def test_pipeline_records_component_durations(mocker):
    """
    Contract Test: Verifies that the pipeline correctly uses the telemetry service. ⏱️✨
    """
    # Arrange
    mock_bus = mocker.Mock()
    mock_telemetry = mocker.Mock(spec=ITelemetryService)
    
    pipeline = AudioProcessingPipeline(
        audio_processor=mocker.Mock(),
        transcriber=mocker.Mock(),
        diarizer=mocker.Mock(),
        alignment_service=mocker.Mock(),
        event_bus=mock_bus,
        telemetry_service=mock_telemetry,
        logger=mocker.Mock(),
    )

    mocker.patch("os.path.exists", return_value=True)

    # Act
    pipeline.execute("source.wav", "de")

    # Assert: Verify that timed_step was called! 🛡️⚖️
    assert mock_telemetry.timed_step.called

