import time
from typing import List, Optional
from uuid import UUID
from src.domain.interfaces import ITranscriber, IDiarizer, IAudioProcessor, IAudioEnricher, ILogger, NullLogger, IAlignmentService
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
        enrichers: List[IAudioEnricher] = None
    ):
        self.audio_processor = audio_processor
        self.transcriber = transcriber
        self.diarizer = diarizer
        self.alignment_service = alignment_service
        self.logger = logger
        self.enrichers = enrichers or []

    def execute(self, source_path: str, language: str, diarization_options: DiarizationOptions = None) -> ProcessingJob:
        job = ProcessingJob(source_path=source_path, target_language=LanguageTag(language))
        start_time = time.time()
        
        try:
            self.logger.info(f"📦 Ingesting & Normalizing {source_path}...")
            job.status = JobStatus.INGESTED
            artifact = self.audio_processor.normalize(source_path)
            self.logger.debug(f"Normalized artifact created at {artifact.file_path}")
            
            self.logger.info(f"🎤 Transcribing with language {language}...")
            job.status = JobStatus.TRANSCRIBING
            raw_utterances = self.transcriber.transcribe(artifact, job.target_language) or []
            self.logger.debug(f"Raw transcription found {len(raw_utterances)} segments.")
            
            self.logger.info(f"🕵️‍♀️ Diarizing audio turns...")
            job.status = JobStatus.DIARIZING
            diarized_segments = self.diarizer.diarize(artifact, options=diarization_options) or []
            self.logger.debug(f"Diarization found {len(diarized_segments)} turns.")
            
            self.logger.info(f"🧩 Aligning words to speakers...")
            final_utterances = self.alignment_service.align(raw_utterances, diarized_segments)
            
            if self.enrichers:
                self.logger.info(f"✨ Running {len(self.enrichers)} enrichers...")
                job.status = JobStatus.ENRICHING
                for enricher in self.enrichers:
                    final_utterances = enricher.enrich(final_utterances, job.target_language)
            
            job.complete(AudioTranscript(utterances=final_utterances))
            
            total_time = time.time() - start_time
            self.logger.info(f"✅ Pipeline completed successfully for {job.id}!")
            self.logger.info(f"⏱️ Total processing time: {self._format_duration(total_time)}")
            
        except Exception as e:
            self.logger.error(f"❌ Pipeline failed for {job.id}: {str(e)}")
            job.status = JobStatus.FAILED
            job.error_message = str(e)
            
        return job

    def _format_duration(self, seconds: float) -> str:
        """Converts raw seconds into a beautiful, human-readable string! 🎀✨"""
        hrs = int(seconds // 3600)
        mins = int((seconds % 3600) // 60)
        secs = int(seconds % 60)
        
        parts = []
        if hrs > 0: parts.append(f"{hrs} hour{'s' if hrs != 1 else ''}")
        if mins > 0: parts.append(f"{mins} minute{'s' if mins != 1 else ''}")
        if secs > 0 or not parts: parts.append(f"{secs} second{'s' if secs != 1 else ''}")
        
        return " ".join(parts)
