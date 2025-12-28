from src.domain.events import (
    AudioIngested,
    SpeechTranscribed,
    SpeakersIdentified,
    JobCompleted,
    JobFailed,
    EnrichmentStarted,
    PipelineStepTimed,
    DomainEvent,
)
from src.domain.interfaces import ILogger, IEventBus


class LoggingEventHandler:
    """
    Subscribes to domain events and centralizes pipeline logging.
    Provides surgical granularity and stack-trace origin for troubleshooting! 🏛️📝✨
    """

    def __init__(self, logger: ILogger, bus: IEventBus):
        self.logger = logger
        self.bus = bus
        self._subscribe_all()

    def _subscribe_all(self):
        self.bus.subscribe(AudioIngested, self.handle_audio_ingested)
        self.bus.subscribe(SpeechTranscribed, self.handle_speech_transcribed)
        self.bus.subscribe(SpeakersIdentified, self.handle_speakers_identified)
        self.bus.subscribe(EnrichmentStarted, self.handle_enrichment_started)
        self.bus.subscribe(PipelineStepTimed, self.handle_step_timed)
        self.bus.subscribe(JobCompleted, self.handle_job_completed)
        self.bus.subscribe(JobFailed, self.handle_job_failed)

    def _tag(self, event: DomainEvent) -> str:
        """Creates a grep-friendly tag including Job ID and source origin. 🆔📍"""
        job_id_short = str(event.job_id)[:8] if hasattr(event, "job_id") else "global"
        origin = f"{event.origin_file}:{event.origin_line}"
        return f"[{job_id_short}] [{origin}]"

    def handle_audio_ingested(self, event: AudioIngested):
        tag = self._tag(event)
        self.logger.info(f"{tag} 📦 Audio Ingested: {event.source_path}")

    def handle_speech_transcribed(self, event: SpeechTranscribed):
        tag = self._tag(event)
        self.logger.info(
            f"{tag} 🎤 Finished: {event.utterance_count} segments found in {event.language}."
        )

    def handle_speakers_identified(self, event: SpeakersIdentified):
        tag = self._tag(event)
        self.logger.info(
            f"{tag} 🕵️‍♀️ Finished: {event.speaker_count} speaker turns identified."
        )

    def handle_enrichment_started(self, event: EnrichmentStarted):
        tag = self._tag(event)
        self.logger.info(f"{tag} ✨ Started: {event.enricher_name}")

    def handle_step_timed(self, event: PipelineStepTimed):
        """Centralized timing log with origin context! ⏱️📈✅"""
        tag = self._tag(event)
        duration_str = self._format_duration(event.duration_seconds)
        self.logger.info(f"{tag} ⏹️ Finished {event.step_name} in {duration_str}")

    def handle_job_completed(self, event: JobCompleted):
        tag = self._tag(event)
        self.logger.info(
            f"{tag} ✅ Completed: {event.utterance_count} final utterances produced."
        )

    def handle_job_failed(self, event: JobFailed):
        tag = self._tag(event)
        self.logger.error(f"{tag} ❌ Failed: {event.error_message}")

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
