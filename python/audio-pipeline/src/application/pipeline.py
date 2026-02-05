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
    IEventPublisher,
    ITelemetryService,
)
from src.domain.entities import ProcessingJob, JobStatus
from src.domain.value_objects import LanguageTag, DiarizationOptions, AudioTranscript
from src.domain.events import (
    AudioIngested,
    SpeechTranscribed,
    SpeakersIdentified,
    JobCompleted,
    JobFailed,
    EnrichmentStarted,
    PipelineStepTimed,
)


class AudioProcessingPipeline:
    def __init__(
        self,
        audio_processor: IAudioProcessor,
        transcriber: ITranscriber,
        diarizer: IDiarizer,
        alignment_service: IAlignmentService,
        event_bus: IEventPublisher,
        telemetry_service: ITelemetryService,
        logger: ILogger,
        enrichers: List[IAudioEnricher] = None,
    ):
        self.audio_processor = audio_processor
        self.transcriber = transcriber
        self.diarizer = diarizer
        self.alignment_service = alignment_service
        self.event_bus = event_bus
        self.telemetry_service = telemetry_service
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
            with self.telemetry_service.timed_step(job.id, "📦 Ingestion & Normalization"):
                job.mark_ingested()
                self.event_bus.publish(
                    AudioIngested(job_id=job.id, source_path=job.source_path)
                )
                artifact = self.audio_processor.normalize(source_path)

            with self.telemetry_service.timed_step(job.id, f"🎤 Transcription ({language})"):
                job.mark_transcribing()
                raw_utterances = (
                    self.transcriber.transcribe(artifact, job.target_language) or []
                )
                self.event_bus.publish(
                    SpeechTranscribed(
                        job_id=job.id,
                        utterance_count=len(raw_utterances),
                        language=job.target_language,
                    )
                )

            with self.telemetry_service.timed_step(job.id, "🕵️‍♀️ Diarization"):
                job.mark_diarizing()
                diarized_segments = (
                    self.diarizer.diarize(artifact, options=diarization_options) or []
                )
                self.event_bus.publish(
                    SpeakersIdentified(job_id=job.id, speaker_count=len(diarized_segments))
                )

            with self.telemetry_service.timed_step(job.id, "🧩 Alignment"):
                final_utterances = self.alignment_service.align(
                    raw_utterances, diarized_segments
                )

            if self.enrichers:
                for i, enricher in enumerate(self.enrichers):
                    enricher_name = (
                        enricher.__class__.__name__
                        if hasattr(enricher, "__class__")
                        else f"Enricher #{i+1}"
                    )
                    with self.telemetry_service.timed_step(job.id, f"✨ Enrichment: {enricher_name}"):
                        job.mark_enriching()
                        self.event_bus.publish(
                            EnrichmentStarted(job_id=job.id, enricher_name=enricher_name)
                        )
                        final_utterances = enricher.enrich(
                            final_utterances, job.target_language
                        )

            job.complete(AudioTranscript(utterances=final_utterances))
            self.event_bus.publish(
                JobCompleted(job_id=job.id, utterance_count=len(final_utterances))
            )

            total_duration = time.time() - total_start_time
            self.logger.info(
                f"⏱️ Total processing duration: {total_duration:.2f}s"
            )

        except Exception as e:
            job.fail(str(e))
            self.event_bus.publish(JobFailed(job_id=job.id, error_message=str(e)))

        return job

