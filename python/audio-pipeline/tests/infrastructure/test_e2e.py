import os
import pytest
import tempfile
import shutil
from dotenv import load_dotenv

load_dotenv()

from src.infrastructure.transcription import WhisperTranscriber
from src.infrastructure.audio import FFmpegAudioProcessor
from src.infrastructure.diarization import PyannoteDiarizer
from src.infrastructure.llama_cpp_translation import LlamaCppTranslator
from src.infrastructure.logging import StandardLogger
from src.application.pipeline import AudioProcessingPipeline
from src.application.services import MaxOverlapAlignmentService
from src.application.enrichers.segmentation import SentenceSegmentationEnricher
from src.application.enrichers.merging import TokenMergerEnricher
from src.application.enrichers.translation import TranslationEnricher
from src.domain.entities import JobStatus
from src.domain.value_objects import LanguageTag

RUN_E2E = os.environ.get("RUN_E2E", "false").lower() == "true"


@pytest.mark.skipif(
    not RUN_E2E, reason="Skipping slow SOTA E2E test. Set RUN_E2E=true to run!"
)
def test_pipeline_end_to_end_real_components():
    # Arrange
    logger = StandardLogger(name="E2ETest")
    audio_processor = FFmpegAudioProcessor()
    transcriber = WhisperTranscriber(
        executable_path="/home/user/Documents/GitHub/whisper.cpp/build/bin/whisper-cli",
        model_path="/home/user/Documents/GitHub/whisper.cpp/models/ggml-large-v3.bin",
    )
    diarizer = PyannoteDiarizer()
    translator = LlamaCppTranslator(
        model_path="models/llama-3.1-8b-instruct-q4_k_m.gguf",
        executable_path="/home/user/Documents/GitHub/llama.cpp/build/bin/llama-cli",
        grammar_path="src/infrastructure/grammars/translation.gbnf",
        logger=logger,
    )

    # Use production enrichment chain 🧩✨
    enrichers = [
        SentenceSegmentationEnricher(max_duration_seconds=3.0, logger=logger),
        TokenMergerEnricher(),
        TranslationEnricher(
            translator=translator, 
            target_lang=LanguageTag("en"), 
            context_size=3,
            logger=logger
        ),
    ]

    pipeline = AudioProcessingPipeline(
        audio_processor=audio_processor,
        transcriber=transcriber,
        diarizer=diarizer,
        alignment_service=MaxOverlapAlignmentService(),
        enrichers=enrichers,
        logger=logger,
    )

    # Act
    with tempfile.TemporaryDirectory() as tmp_dir:
        original_source = os.path.join(
            os.path.dirname(__file__), "../data/test_10s.m4a"
        )
        temp_source = os.path.join(tmp_dir, "test_10s.m4a")
        shutil.copy(original_source, temp_source)

        job = pipeline.execute(temp_source, "de")

        # Assert
        assert job.status == JobStatus.COMPLETED
        assert job.result is not None
        assert len(job.result.utterances) > 0

        # With 3s threshold, the 10s audio should have several utterances! 📈
        assert (
            len(job.result.utterances) >= 2
        ), "Segmentation failed to produce granular utterances!"

        first_utterance = job.result.utterances[0]
        assert len(first_utterance.words) > 0, "No words found in the first utterance!"

        assert "hallo" in first_utterance.text.lower()
        assert first_utterance.speaker_id.startswith("SPEAKER_")
        
        # Verify Translation! 🇩🇪 -> 🇺🇸
        assert first_utterance.translated_text is not None, "Translation missing!"
        assert len(first_utterance.translated_text) > 0, "Translation is empty!"
