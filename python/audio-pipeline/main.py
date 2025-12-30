import os
import argparse
from dotenv import load_dotenv
from src.infrastructure.logging import StandardLogger
from src.infrastructure.serialization import JsonTranscriptSerializer
from src.infrastructure.repositories import FileSystemResultRepository
from src.application.pipeline import AudioProcessingPipeline
from src.infrastructure.bus import InProcessEventBus
from src.infrastructure.event_handlers import LoggingEventHandler
from src.infrastructure.factory import PipelineComponentFactory
from src.domain.entities import JobStatus
from src.domain.value_objects import LanguageTag
import logging


def main():
    load_dotenv()

    parser = argparse.ArgumentParser(description="Audio Pipeline CLI 🎙️✨")
    parser.add_argument("input", help="Path to the source audio file")
    parser.add_argument(
        "--output-dir", default="./output", help="Directory for results and temp files"
    )
    parser.add_argument(
        "--language",
        default="de-DE",
        help="Target language code (BCP-47 for Azure, e.g., de-DE)",
    )
    parser.add_argument(
        "--target-language", default="en", help="Language to translate into"
    )
    parser.add_argument("--num-speakers", type=int, help="Expected number of speakers")
    parser.add_argument(
        "--max-duration",
        type=float,
        default=15.0,
        help="Max duration in seconds for sentence segmentation",
    )
    parser.add_argument(
        "--translation-context",
        type=int,
        default=10,
        help="Number of preceding utterances to provide as context for translation",
    )
    parser.add_argument(
        "--translation-batch",
        type=int,
        default=1,
        help="Number of utterances to translate in a single block (Must be 1 to ensure 1:1 alignment and prevent LLM merging)",
    )
    parser.add_argument(
        "--use-azure",
        action="store_true",
        help="Use full Azure cloud-native pipeline (Transcription & Foundry Translation). ☁️🏎️💨",
    )

    args = parser.parse_args()

    # Ensure output directories exist
    os.makedirs(args.output_dir, exist_ok=True)
    temp_dir = os.path.join(args.output_dir, "temp")
    os.makedirs(temp_dir, exist_ok=True)

    # 🏛️ Composition Root Factory Setup
    log_file_path = os.path.join(args.output_dir, "pipeline.log")
    logger = StandardLogger(
        name="Pipeline", log_file=log_file_path, level=logging.DEBUG
    )

    # ⚡️ Reactive Event Bus Setup
    event_bus = InProcessEventBus()
    LoggingEventHandler(logger=logger, bus=event_bus)

    # 🏗️ Build Components using Factory
    factory = PipelineComponentFactory(args, logger)
    (
        audio_processor,
        transcriber,
        diarizer,
        alignment_service,
        enrichers,
    ) = factory.build_components()

    serializer = JsonTranscriptSerializer()
    result_repo = FileSystemResultRepository(serializer=serializer)

    pipeline = AudioProcessingPipeline(
        audio_processor=audio_processor,
        transcriber=transcriber,
        diarizer=diarizer,
        alignment_service=alignment_service,
        event_bus=event_bus,
        logger=logger,
        enrichers=enrichers,
    )

    # 3. Execute
    job = pipeline.execute(
        source_path=args.input,
        language=args.language,
    )

    if job.status == JobStatus.FAILED:
        logger.error(f"❌ Job failed! {job.error_message}")
    else:
        output_path = os.path.join(args.output_dir, "transcript.json")
        result_repo.save(job.result, output_path)
        logger.info(f"💾 Results saved to {output_path}! 💎")


if __name__ == "__main__":
    main()
