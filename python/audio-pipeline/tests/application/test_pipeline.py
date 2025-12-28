from unittest.mock import Mock
from src.application.pipeline import AudioProcessingPipeline
from src.domain.interfaces import (
    ITranscriber,
    IDiarizer,
    IAudioProcessor,
    ILogger,
    IAlignmentService,
    IEventBus,
)
from src.domain.entities import JobStatus


def test_pipeline_execution_flow(mocker):
    # Arrange
    mock_audio_processor = mocker.Mock(spec=IAudioProcessor)
    mock_transcriber = mocker.Mock(spec=ITranscriber)
    mock_transcriber.transcribe.return_value = []
    mock_diarizer = mocker.Mock(spec=IDiarizer)
    mock_diarizer.diarize.return_value = []
    mock_alignment_service = mocker.Mock(spec=IAlignmentService)
    mock_alignment_service.align.return_value = []
    mock_event_bus = mocker.Mock(spec=IEventBus)

    pipeline = AudioProcessingPipeline(
        audio_processor=mock_audio_processor,
        transcriber=mock_transcriber,
        diarizer=mock_diarizer,
        alignment_service=mock_alignment_service,
        event_bus=mock_event_bus,
        logger=mocker.Mock(spec=ILogger),
    )

    # We mock the system boundary sanity check! 🛡️⚖️
    mocker.patch("os.path.exists", return_value=True)

    # Act
    job = pipeline.execute("source.m4a", "de")

    # Assert
    assert job.status == JobStatus.COMPLETED
    assert job.result is not None
    assert mock_event_bus.publish.called
    mock_audio_processor.normalize.assert_called_once_with("source.m4a")
    mock_transcriber.transcribe.assert_called_once()
    mock_diarizer.diarize.assert_called_once()
    mock_alignment_service.align.assert_called_once()


def test_pipeline_failure_handles_exceptions(mocker):
    processor = mocker.Mock(spec=IAudioProcessor)
    processor.normalize.side_effect = Exception("Boom! 💥")
    mock_event_bus = mocker.Mock(spec=IEventBus)

    pipeline = AudioProcessingPipeline(
        audio_processor=processor,
        transcriber=mocker.Mock(),
        diarizer=mocker.Mock(),
        alignment_service=mocker.Mock(),
        event_bus=mock_event_bus,
        logger=mocker.Mock(),
    )

    mocker.patch("os.path.exists", return_value=True)

    job = pipeline.execute("source.m4a", "de")

    assert job.status == JobStatus.FAILED
    assert "Boom! 💥" in job.error_message
    assert mock_event_bus.publish.called
