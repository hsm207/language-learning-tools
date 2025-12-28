import time
import os
from typing import List, Optional, Generator
from uuid import UUID
from contextlib import contextmanager
from src.domain.interfaces import (
    ITranscriber,
    IDiarizer,
    IAudioProcessor,
    IAudioEnricher,
    ILogger,
    IAlignmentService,
    IEventBus,
)
from src.infrastructure.logging import NullLogger
from src.domain.entities import ProcessingJob, JobStatus
from src.domain.value_objects import LanguageTag, DiarizationOptions, AudioTranscript


class AudioProcessingPipeline:
    def __init__(
        self,
        audio_processor: IAudioProcessor,
        transcriber: ITranscriber,
        diarizer: IDiarizer,
        alignment_service: IAlignmentService,
        event_bus: IEventBus,
        logger: ILogger = NullLogger(),
        enrichers: List[IAudioEnricher] = None,
    ):
        self.audio_processor = audio_processor
        self.transcriber = transcriber
        self.diarizer = diarizer
        self.alignment_service = alignment_service
        self.event_bus = event_bus
        self.logger = logger
        self.enrichers = enrichers or []

    def execute(
        self,
        source_path: str,
        language: str,
        diarization_options: DiarizationOptions = None,
    ) -> ProcessingJob:
        if not os.path.exists(source_path):
            raise FileNotFoundError(f"Source audio file not found: {source_path}")

        if not language:
            raise ValueError("Target language must be provided!")

        job = ProcessingJob(
            source_path=source_path, target_language=LanguageTag(language)
        )
        total_start_time = time.time()

        try:
            with self._timed_step(job, "📦 Ingestion & Normalization"):
                job.mark_ingested()
                artifact = self.audio_processor.normalize(source_path)
                self._flush_events(job)

            with self._timed_step(job, f"🎤 Transcription ({language})"):
                job.mark_transcribing()
                raw_utterances = (
                    self.transcriber.transcribe(artifact, job.target_language) or []
                )
                job.record_transcription_finished(
                    len(raw_utterances), job.target_language
                )
                self._flush_events(job)

            with self._timed_step(job, "🕵️‍♀️ Diarization"):
                job.mark_diarizing()
                diarized_segments = (
                    self.diarizer.diarize(artifact, options=diarization_options) or []
                )
                job.record_diarization_finished(len(diarized_segments))
                self._flush_events(job)

            with self._timed_step(job, "🧩 Alignment"):
                final_utterances = self.alignment_service.align(
                    raw_utterances, diarized_segments
                )
                self._flush_events(job)

            if self.enrichers:
                for i, enricher in enumerate(self.enrichers):
                    enricher_name = (
                        enricher.__class__.__name__
                        if hasattr(enricher, "__class__")
                        else f"Enricher #{i+1}"
                    )
                    with self._timed_step(job, f"✨ Enrichment: {enricher_name}"):
                        job.mark_enriching(enricher_name)
                        self._flush_events(job)
                        final_utterances = enricher.enrich(
                            final_utterances, job.target_language
                        )

            job.complete(AudioTranscript(utterances=final_utterances))
            self._flush_events(job)

            total_duration = time.time() - total_start_time
            self.logger.info(
                f"⏱️ Total processing time: {self._format_duration(total_duration)}"
            )

        except Exception as e:
            job.fail(str(e))
            self._flush_events(job)

        return job

    def _flush_events(self, job: ProcessingJob):
        """Dispatches all pending events from the job to the event bus. ⚡️"""
        for event in job.pull_events():
            self.event_bus.publish(event)

    @contextmanager
    def _timed_step(self, job: ProcessingJob, step_name: str) -> Generator[None, None, None]:
        """A context manager to record component duration as a domain event. ⏳✨"""
        start_time = time.time()
        try:
            yield
        finally:
            duration = time.time() - start_time
            job.record_step_duration(step_name, duration)

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
