import os
import pytest
from src.infrastructure.transcription import WhisperTranscriber
from src.infrastructure.audio import FFmpegAudioProcessor
from src.infrastructure.diarization import PyannoteDiarizer
from src.application.pipeline import AudioProcessingPipeline
from src.application.services import AlignmentService
from src.domain.entities import JobStatus

# Only run this if we explicitly want to wait for the real models! 🦕💎
RUN_E2E = os.environ.get("RUN_E2E", "false").lower() == "true"

@pytest.mark.skipif(not RUN_E2E, reason="Skipping slow SOTA E2E test. Set RUN_E2E=true to run! 🚀")
def test_pipeline_end_to_end_real_components():
    # 1. Setup real SOTA infrastructure
    audio_processor = FFmpegAudioProcessor()
    transcriber = WhisperTranscriber(
        executable_path="/home/user/Documents/GitHub/whisper.cpp/build/bin/whisper-cli",
        model_path="/home/user/Documents/GitHub/whisper.cpp/models/ggml-large-v3.bin"
    )
    # Pyannote will use HF_TOKEN from env
    diarizer = PyannoteDiarizer()
    
    pipeline = AudioProcessingPipeline(
        audio_processor=audio_processor,
        transcriber=transcriber,
        diarizer=diarizer,
        alignment_service=AlignmentService()
    )
    
    # 2. Execute on 10s snippet
    source_path = os.path.join(os.path.dirname(__file__), "../data/test_10s.m4a")
    job = pipeline.execute(source_path, "de")
    
    # 3. Assert high-fidelity results 💎
    assert job.status == JobStatus.COMPLETED
    assert len(job.utterances) > 0
    # Peter usually starts the first lesson!
    assert "hallo" in job.utterances[0].text.lower()
    assert job.utterances[0].speaker_id.startswith("SPEAKER_")
