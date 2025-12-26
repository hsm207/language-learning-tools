from typing import List
from src.application.services import AlignmentService
from src.domain.interfaces import ITranscriber, IDiarizer, IAudioProcessor, IAudioEnricher, ILogger, NullLogger
from src.domain.entities import ProcessingJob, JobStatus
from src.domain.value_objects import LanguageTag

class AudioProcessingPipeline:
    def __init__(
        self,
        audio_processor: IAudioProcessor,
        transcriber: ITranscriber,
        diarizer: IDiarizer,
        alignment_service: AlignmentService,
        logger: ILogger = NullLogger(), # Default to silent! 🤫
        enrichers: List[IAudioEnricher] = None
    ):
        self.audio_processor = audio_processor
        self.transcriber = transcriber
        self.diarizer = diarizer
        self.alignment_service = alignment_service
        self.logger = logger
        self.enrichers = enrichers or []

    def execute(self, source_path: str, language: str) -> ProcessingJob:
        job = ProcessingJob(source_path=source_path, target_language=LanguageTag(language))
        
        try:
            # 1. Ingest & Normalize
            self.logger.info(f"📦 Ingesting & Normalizing {source_path}...")
            job.status = JobStatus.INGESTED
            artifact = self.audio_processor.normalize(source_path)
            self.logger.debug(f"Normalized artifact created at {artifact.file_path}")
            
            # 2. Transcribe & Diarize
            self.logger.info(f"🎤 Transcribing with language {language}...")
            job.status = JobStatus.TRANSCRIBING
            raw_utterances = self.transcriber.transcribe(artifact, job.target_language)
            self.logger.debug(f"Raw transcription found {len(raw_utterances)} segments.")
            
            self.logger.info(f"🕵️‍♀️ Diarizing audio turns...")
            job.status = JobStatus.DIARIZING
            diarized_segments = self.diarizer.diarize(artifact)
            self.logger.debug(f"Diarization found {len(diarized_segments)} turns.")
            
            # 3. Align
            self.logger.info(f"🧩 Aligning words to speakers...")
            final_utterances = self.alignment_service.align(raw_utterances, diarized_segments)
            
            # 4. Enrich
            if self.enrichers:
                self.logger.info(f"✨ Running {len(self.enrichers)} enrichers...")
                job.status = JobStatus.ENRICHING
                for enricher in self.enrichers:
                    final_utterances = enricher.enrich(final_utterances, job.target_language)
            
            job.utterances = final_utterances
            job.status = JobStatus.COMPLETED
            self.logger.info(f"✅ Pipeline completed successfully for {job.id}!")
            
        except Exception as e:
            self.logger.error(f"❌ Pipeline failed for {job.id}: {str(e)}")
            job.status = JobStatus.FAILED
            job.error_message = str(e)
            
        return job
