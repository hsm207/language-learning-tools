import pytest
from uuid import uuid4
from src.infrastructure.event_handlers import LoggingEventHandler
from src.domain.events import PipelineStepTimed


def test_logging_handler_duration_formatting_via_public_api(mocker):
    """
    Contract Test: Verifies that the handler formats durations correctly
    in its log messages by handling real events. ⏱️📈✅
    """
    # Arrange
    mock_logger = mocker.Mock()
    handler = LoggingEventHandler(mock_logger, mocker.Mock())
    job_id = uuid4()

    # We test all duration branches via the public 'handle_step_timed' API! 🏙️💎

    # 1. Milliseconds
    e1 = PipelineStepTimed(job_id=job_id, step_name="Step1", duration_seconds=0.5)
    handler.handle_step_timed(e1)
    assert "Finished Step1 in 500ms" in mock_logger.info.call_args[0][0]

    # 2. Seconds
    e2 = PipelineStepTimed(job_id=job_id, step_name="Step2", duration_seconds=1.5)
    handler.handle_step_timed(e2)
    assert "Finished Step2 in 1.500s" in mock_logger.info.call_args[0][0]

    # 3. Minutes
    e3 = PipelineStepTimed(job_id=job_id, step_name="Step3", duration_seconds=65)
    handler.handle_step_timed(e3)
    assert "Finished Step3 in 1m 5s" in mock_logger.info.call_args[0][0]

    # 4. Hours
    e4 = PipelineStepTimed(job_id=job_id, step_name="Step4", duration_seconds=3665)
    handler.handle_step_timed(e4)
    assert "Finished Step4 in 1h 1m 5s" in mock_logger.info.call_args[0][0]
