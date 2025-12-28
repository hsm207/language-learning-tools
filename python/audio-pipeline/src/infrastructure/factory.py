from typing import List, Tuple
import os
from src.domain.interfaces import (
    ITranscriber,
    IDiarizer,
    IAudioProcessor,
    IAudioEnricher,
    ILogger,
    IAlignmentService,
    ITranslator,
)
from src.domain.value_objects import LanguageTag
from src.infrastructure.transcription import WhisperTranscriber, AzureFastTranscriber
from src.infrastructure.audio import FFmpegAudioProcessor
from src.infrastructure.diarization import PyannoteDiarizer, NullDiarizer
from src.infrastructure.llama_cpp_translation import LlamaCppTranslator
from src.application.services import MaxOverlapAlignmentService
from src.application.enrichers.segmentation import SentenceSegmentationEnricher
from src.application.enrichers.merging import TokenMergerEnricher
from src.application.enrichers.translation import TranslationEnricher


class PipelineComponentFactory:
    """
    Composition Root Factory. 🏗️✨
    Encapsulates the 'Construction Strategy' for different providers to keep the core OCP-compliant! 🛡️⚖️🏛️
    """

    def __init__(self, args, logger: ILogger):
        self.args = args
        self.logger = logger

    def build_components(
        self,
    ) -> Tuple[
        IAudioProcessor,
        ITranscriber,
        IDiarizer,
        IAlignmentService,
        List[IAudioEnricher],
    ]:
        audio_processor = FFmpegAudioProcessor()
        alignment_service = MaxOverlapAlignmentService()

        if self.args.use_azure:
            return self._build_azure_stack(audio_processor, alignment_service)
        else:
            return self._build_local_stack(audio_processor, alignment_service)

    def _build_local_stack(self, audio_processor, alignment_service) -> Tuple[
        IAudioProcessor,
        ITranscriber,
        IDiarizer,
        IAlignmentService,
        List[IAudioEnricher],
    ]:
        self.logger.info("🏠 Local Mode: Using Whisper & Pyannote.")

        transcriber = WhisperTranscriber(
            executable_path="/home/user/Documents/GitHub/whisper.cpp/build/bin/whisper-cli",
            model_path="/home/user/Documents/GitHub/whisper.cpp/models/ggml-large-v3.bin",
            logger=self.logger,
        )
        diarizer = PyannoteDiarizer(logger=self.logger)

        enrichers = self._build_base_enrichers()
        enrichers.insert(1, TokenMergerEnricher())  # Local needs token merging! 🧩

        return audio_processor, transcriber, diarizer, alignment_service, enrichers

    def _build_azure_stack(self, audio_processor, alignment_service) -> Tuple[
        IAudioProcessor,
        ITranscriber,
        IDiarizer,
        IAlignmentService,
        List[IAudioEnricher],
    ]:
        self.logger.info("☁️ Azure Mode: Using Fast Transcription & Null Diarizer!")

        api_key = os.environ.get("AZURE_SPEECH_KEY")
        region = os.environ.get("AZURE_SPEECH_REGION")

        if not api_key or not region:
            raise ValueError("❌ Missing AZURE_SPEECH_KEY or AZURE_SPEECH_REGION! ")

        transcriber = AzureFastTranscriber(
            api_key=api_key, region=region, logger=self.logger
        )
        diarizer = NullDiarizer(logger=self.logger)

        enrichers = self._build_base_enrichers()  # Azure already provides words! 🧼

        return audio_processor, transcriber, diarizer, alignment_service, enrichers

    def _build_base_enrichers(self) -> List[IAudioEnricher]:
        translator = LlamaCppTranslator(
            model_path="models/llama-3.1-8b-instruct-q4_k_m.gguf",
            executable_path="/home/user/Documents/GitHub/llama.cpp/build/bin/llama-cli",
            grammar_path="src/infrastructure/grammars/translation.gbnf",
            logger=self.logger,
        )

        return [
            SentenceSegmentationEnricher(
                max_duration_seconds=self.args.max_duration, logger=self.logger
            ),
            TranslationEnricher(
                translator=translator,
                target_lang=LanguageTag(self.args.target_language),
                context_size=self.args.translation_context,
                batch_size=self.args.translation_batch,
                logger=self.logger,
            ),
        ]
