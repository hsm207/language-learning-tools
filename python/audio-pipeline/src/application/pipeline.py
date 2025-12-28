import time
from typing import List, Optional, Generator
from uuid import UUID
from contextlib import contextmanager
from src.domain.interfaces import (
    ITranscriber,
    IDiarizer,
    IAudioProcessor,
    IAudioEnricher,
    ILogger,
    NullLogger,
    IAlignmentService,
)
from src.domain.entities import ProcessingJob, JobStatus
from src.domain.value_objects import LanguageTag, DiarizationOptions, AudioTranscript


class AudioProcessingPipeline:
    def __init__(
        self,
        audio_processor: IAudioProcessor,
        transcriber: ITranscriber,
        diarizer: IDiarizer,
        alignment_service: IAlignmentService,
        logger: ILogger = NullLogger(),
        enrichers: List[IAudioEnricher] = None,
    ):
        self.audio_processor = audio_processor
        self.transcriber = transcriber
        self.diarizer = diarizer
        self.alignment_service = alignment_service
        self.logger = logger
        self.enrichers = enrichers or []

    def execute(
        self,
        source_path: str,
        language: str,
        diarization_options: DiarizationOptions = None,
    ) -> ProcessingJob:
        job = ProcessingJob(
            source_path=source_path, target_language=LanguageTag(language)
        )
        total_start_time = time.time()

        try:
            with self._timed_step("📦 Ingestion & Normalization"):
                self.logger.info(f"Processing {source_path}...")
                job.status = JobStatus.INGESTED
                artifact = self.audio_processor.normalize(source_path)
                self.logger.debug(f"Normalized artifact: {artifact.file_path}")

            with self._timed_step(f"🎤 Transcription ({language})"):
                job.status = JobStatus.TRANSCRIBING
                raw_utterances = (
                    self.transcriber.transcribe(artifact, job.target_language) or []
                )
                self.logger.debug(f"Found {len(raw_utterances)} segments.")

            with self._timed_step("🕵️‍♀️ Diarization"):
                job.status = JobStatus.DIARIZING
                diarized_segments = (
                    self.diarizer.diarize(artifact, options=diarization_options) or []
                )
                self.logger.debug(f"Found {len(diarized_segments)} turns.")

            with self._timed_step("🧩 Alignment"):
                final_utterances = self.alignment_service.align(
                    raw_utterances, diarized_segments
                )

            if self.enrichers:
                job.status = JobStatus.ENRICHING
                for i, enricher in enumerate(self.enrichers):
                    enricher_name = (
                        enricher.__class__.__name__
                        if hasattr(enricher, "__class__")
                        else f"Enricher #{i+1}"
                    )
                    with self._timed_step(f"✨ Enrichment: {enricher_name}"):
                        final_utterances = enricher.enrich(
                            final_utterances, job.target_language
                        )

            job.complete(AudioTranscript(utterances=final_utterances))

            total_duration = time.time() - total_start_time
            self.logger.info(f"✅ Pipeline completed successfully for {job.id}!")
            self.logger.info(
                f"⏱️ Total processing time: {self._format_duration(total_duration)}"
            )

        except Exception as e:
            self.logger.error(f"❌ Pipeline failed for {job.id}: {str(e)}")
            job.status = JobStatus.FAILED
            job.error_message = str(e)

        return job

    @contextmanager
    def _timed_step(self, step_name: str) -> Generator[None, None, None]:
        """A context manager to measure and log the duration of a pipeline step. ⏳✨"""
        start_time = time.time()
        self.logger.info(f"▶️ Starting {step_name}...")
        try:
            yield
        finally:
            duration = time.time() - start_time
            self.logger.info(
                f"⏹️ Finished {step_name} in {self._format_duration(duration)}"
            )

    def _format_duration(self, seconds: float) -> str:
        """Converts raw seconds into a beautiful, human-readable string! 🎀✨"""
        hrs = int(seconds // 3600)
        mins = int((seconds % 3600) // 60)
        secs = int(seconds % 60)
        ms = int((seconds * 1000) % 1000)

        if hrs > 0:
            return f"{hrs}h {mins}m {secs}s"
        if mins > 0:
            return f"{mins}m {secs}s"
        if secs > 0:
            return f"{secs}.{ms:03d}s"
        return f"{ms}ms"