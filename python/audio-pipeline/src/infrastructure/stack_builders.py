from abc import ABC, abstractmethod
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
    ITelemetryService,
)
from src.domain.value_objects import LanguageTag
from src.infrastructure.transcription import WhisperTranscriber, AzureFastTranscriber
from src.infrastructure.audio import FFmpegAudioProcessor
from src.infrastructure.diarization import PyannoteDiarizer, NullDiarizer
from src.infrastructure.llama_cpp_translation import LlamaCppTranslator
from src.infrastructure.azure_inference_translation import AzureInferenceTranslator
from src.application.services import MaxOverlapAlignmentService
from src.application.enrichers.segmentation import SentenceSegmentationEnricher
from src.application.enrichers.merging import TokenMergerEnricher
from src.application.enrichers.translation import TranslationEnricher
from src.application.enrichers.annotation import LinguisticAnnotationEnricher
from src.infrastructure.azure_inference_annotation import AzureInferenceAnnotationService


class IStackBuilder(ABC):
    """
    Contract for building a specific component stack (e.g., Local, Azure). 🏗️✨
    """

    @abstractmethod
    def build(
        self, args, logger: ILogger
    ) -> Tuple[
        IAudioProcessor,
        ITranscriber,
        IDiarizer,
        IAlignmentService,
        List[IAudioEnricher],
    ]:
        pass


class LocalStackBuilder(IStackBuilder):
    def build(
        self, args, logger: ILogger
    ) -> Tuple[
        IAudioProcessor,
        ITranscriber,
        IDiarizer,
        IAlignmentService,
        List[IAudioEnricher],
    ]:
        logger.info("🏠 Local Mode: Using Whisper & Pyannote.")

        audio_processor = FFmpegAudioProcessor()
        alignment_service = MaxOverlapAlignmentService()

        transcriber = WhisperTranscriber(
            executable_path="/home/user/Documents/GitHub/whisper.cpp/build/bin/whisper-cli",
            model_path="/home/user/Documents/GitHub/whisper.cpp/models/ggml-large-v3.bin",
            logger=logger,
        )
        diarizer = PyannoteDiarizer(logger=logger)

        # Build local translator
        translator = LlamaCppTranslator(
            model_path="models/llama-3.1-8b-instruct-q4_k_m.gguf",
            executable_path="/home/user/Documents/GitHub/llama.cpp/build/bin/llama-cli",
            grammar_path="src/infrastructure/grammars/translation.gbnf",
            logger=logger,
        )

        enrichers: List[IAudioEnricher] = [
            SentenceSegmentationEnricher(
                max_duration_seconds=args.max_duration, logger=logger
            ),
            TokenMergerEnricher(),  # Local needs token merging 🧩
            TranslationEnricher(
                translator=translator,
                target_lang=LanguageTag(args.target_language),
                context_size=args.translation_context,
                batch_size=args.translation_batch,
                logger=logger,
            ),
        ]

        return audio_processor, transcriber, diarizer, alignment_service, enrichers


class AzureStackBuilder(IStackBuilder):
    def build(
        self, args, logger: ILogger
    ) -> Tuple[
        IAudioProcessor,
        ITranscriber,
        IDiarizer,
        IAlignmentService,
        List[IAudioEnricher],
    ]:
        logger.info("☁️ Azure Mode: Using Fast Transcription & Null Diarizer!")

        speech_key = os.environ.get("AZURE_SPEECH_KEY")
        speech_region = os.environ.get("AZURE_SPEECH_REGION")

        if not speech_key or not speech_region:
            raise ValueError(
                "❌ Missing AZURE_SPEECH_KEY or AZURE_SPEECH_REGION! 🛡️⚖️🏛️"
            )

        audio_processor = FFmpegAudioProcessor()
        alignment_service = MaxOverlapAlignmentService()

        transcriber = AzureFastTranscriber(
            api_key=speech_key, region=speech_region, logger=logger
        )
        diarizer = NullDiarizer(logger=logger)

        # Build Azure translator
        foundry_key = os.environ.get("AZURE_AI_INFERENCE_KEY")
        foundry_endpoint = os.environ.get("AZURE_AI_INFERENCE_ENDPOINT")

        if not foundry_key or not foundry_endpoint:
            raise ValueError(
                "❌ Missing AZURE_AI_INFERENCE_KEY or AZURE_AI_INFERENCE_ENDPOINT! 🛡️⚖️🏛️"
            )

        translator = AzureInferenceTranslator(
            api_key=foundry_key, endpoint=foundry_endpoint, logger=logger
        )

        enrichers: List[IAudioEnricher] = [
            SentenceSegmentationEnricher(
                max_duration_seconds=args.max_duration, logger=logger
            ),
            TranslationEnricher(
                translator=translator,
                target_lang=LanguageTag(args.target_language),
                context_size=args.translation_context,
                batch_size=args.translation_batch,
                logger=logger,
            ),
        ]

        # 🎓 Pedagogical Layer: Add linguistic annotation for Azure! 💎✨
        annotation_service = AzureInferenceAnnotationService(
            api_key=foundry_key, endpoint=foundry_endpoint, logger=logger
        )
        enrichers.append(
            LinguisticAnnotationEnricher(
                annotation_service=annotation_service,
                batch_size=args.annotation_batch,
                context_size=args.annotation_context,
                logger=logger,
            )
        )

        return audio_processor, transcriber, diarizer, alignment_service, enrichers
