import os
import pytest
import tempfile
import shutil
from dotenv import load_dotenv
load_dotenv() # Load your detailed HF_TOKEN! 🔐✨

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
    
    # 2. Execute on 10s snippet in a temp directory 🧼✨
    with tempfile.TemporaryDirectory() as tmp_dir:
        original_source = os.path.join(os.path.dirname(__file__), "../data/test_10s.m4a")
        temp_source = os.path.join(tmp_dir, "test_10s.m4a")
        shutil.copy(original_source, temp_source)
        
        job = pipeline.execute(temp_source, "de")
        
        # 3. Assert high-fidelity results 💎
        assert job.status == JobStatus.COMPLETED
        assert len(job.utterances) > 0
        
        # Check for word-level data 🎶✨
        first_utterance = job.utterances[0]
        assert len(first_utterance.words) > 0, "No words found in the first utterance! 😱"
        
        # We trust the Domain Layer to have enforced word/utterance alignment invariants! 🛡️🏛️
        # Peter usually starts the first lesson!
        assert "hallo" in first_utterance.text.lower()
        assert first_utterance.speaker_id.startswith("SPEAKER_")
