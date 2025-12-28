import pytest
from datetime import timedelta
from unittest.mock import Mock
from src.application.pipeline import AudioProcessingPipeline
from src.domain.entities import ProcessingJob, JobStatus
from src.domain.events import PipelineStepTimed


def test_pipeline_records_component_durations(mocker):
    """
    Contract Test: Verifies that the pipeline correctly measures and
    records the duration of its components via domain events. ⏱️✨
    """
    # Arrange
    mock_bus = mocker.Mock()
    pipeline = AudioProcessingPipeline(
        audio_processor=mocker.Mock(),
        transcriber=mocker.Mock(),
        diarizer=mocker.Mock(),
        alignment_service=mocker.Mock(),
        event_bus=mock_bus,
    )

    mocker.patch("os.path.exists", return_value=True)

    # We simulate a "Very Long" process to hit the 'h/m/s' formatting indirectly! 🏎️💨
    # Side effects: [TotalStart, StepStart, StepEnd, ...]
    mocker.patch("time.time", side_effect=[0, 0, 3661, 0, 0, 0, 0, 0, 0, 0, 0, 0])

    # Act
    pipeline.execute("source.wav", "de")

    # Assert: Check the recorded event! 🛡️⚖️
    events = [call.args[0] for call in mock_bus.publish.call_args_list]
    timed_events = [e for event in events if isinstance(e := event, PipelineStepTimed)]

    assert len(timed_events) > 0
    # The first step (Ingestion) took 3661s according to our side_effect (3661 - 0)
    assert timed_events[0].duration_seconds == 3661
