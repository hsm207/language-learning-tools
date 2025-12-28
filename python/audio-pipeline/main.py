import os
import argparse
from dotenv import load_dotenv
from src.infrastructure.transcription import WhisperTranscriber
from src.infrastructure.audio import FFmpegAudioProcessor
from src.infrastructure.diarization import PyannoteDiarizer
from src.infrastructure.logging import LocalLogger
from src.infrastructure.serialization import JsonTranscriptSerializer
from src.application.pipeline import AudioProcessingPipeline
from src.application.services import MaxOverlapAlignmentService
from src.application.enrichers import (
    SentenceSegmentationEnricher,
    TokenMergerEnricher,
    TranslationEnricher,
)
from src.infrastructure.llama_cpp_translation import LlamaCppTranslator
from src.domain.value_objects import DiarizationOptions, LanguageTag


def main():
    load_dotenv()

    parser = argparse.ArgumentParser(description="SOTA Audio Pipeline CLI 🎙️✨")
    parser.add_argument("input", help="Path to the source audio file")
    parser.add_argument(
        "--output-dir", default="./output", help="Directory for results and temp files"
    )
    parser.add_argument(
        "--language", default="de", help="Target language code (e.g., de, en)"
    )
    parser.add_argument(
        "--target-language", default="en", help="Language to translate into"
    )
    parser.add_argument(
        "--num-speakers", type=int, help="Expected number of speakers"
    )
    parser.add_argument(
        "--max-duration",
        type=float,
        default=15.0,
        help="Max duration in seconds for sentence segmentation",
    )
    parser.add_argument(
        "--translation-context",
        type=int,
        default=3,
        help="Number of preceding utterances to provide as context for translation",
    )
    parser.add_argument(
        "--translation-batch",
        type=int,
        default=1,
        help="Number of utterances to translate in a single block (currently 1 for best accuracy)",
    )

    args = parser.parse_args()

    # Ensure output directories exist
    os.makedirs(args.output_dir, exist_ok=True)
    temp_dir = os.path.join(args.output_dir, "temp")
    os.makedirs(temp_dir, exist_ok=True)

    # 1. Setup Infrastructure
    logger = LocalLogger(log_file=os.path.join(args.output_dir, "pipeline.log"))
    audio_processor = FFmpegAudioProcessor(
        work_dir=temp_dir, logger=logger.get_child("AudioProcessor")
    )
    transcriber = WhisperTranscriber(
        executable_path="/home/user/Documents/GitHub/whisper.cpp/build/bin/whisper-cli",
        model_path="/home/user/Documents/GitHub/whisper.cpp/models/ggml-large-v3.bin",
        logger=logger.get_child("Transcriber"),
    )
    diarizer = PyannoteDiarizer(logger=logger.get_child("Diarizer"))
    
    # Llama Translator Configuration 🦕💎
    translator = LlamaCppTranslator(
        model_path="models/llama-3.1-8b-instruct-q4_k_m.gguf",
        executable_path="/home/user/Documents/GitHub/llama.cpp/build/bin/llama-cli",
        grammar_path="src/infrastructure/grammars/translation.gbnf",
        logger=logger.get_child("Translator"),
    )
    serializer = JsonTranscriptSerializer()

    # 2. Setup Application
    enrichers = [
        SentenceSegmentationEnricher(
            max_duration_seconds=args.max_duration, logger=logger.get_child("SentenceSegmenter")
        ),
        TokenMergerEnricher(),
        TranslationEnricher(
            translator=translator,
            target_lang=LanguageTag(args.target_language),
            context_size=args.translation_context,
            batch_size=args.translation_batch,
            logger=logger.get_child("TranslationEnricher"),
        ),
    ]

    pipeline = AudioProcessingPipeline(
        audio_processor=audio_processor,
        transcriber=transcriber,
        diarizer=diarizer,
        alignment_service=MaxOverlapAlignmentService(),
        logger=logger.get_child("Orchestrator"),
        enrichers=enrichers,
    )

    # 3. Execute
    logger.info(f"🚀 Starting SOTA Audio Pipeline for {args.input}...")
    options = DiarizationOptions(num_speakers=args.num_speakers)
    job = pipeline.execute(args.input, args.language, diarization_options=options)

    if job.error_message:
        logger.error(f"❌ Job failed! {job.error_message}")
    else:
        logger.info(
            f"✨ Job {job.id} completed successfully with {len(job.utterances)} utterances!"
        )

        output_json_path = os.path.join(args.output_dir, "transcript.json")
        json_content = serializer.serialize(job.result)

        with open(output_json_path, "w", encoding="utf-8") as f:
            f.write(json_content)

        logger.info(f"💾 Results saved to {output_json_path}! 💎")


if __name__ == "__main__":
    main()
