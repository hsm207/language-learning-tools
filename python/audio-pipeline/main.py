from dotenv import load_dotenv
load_dotenv() # Load your detailed HF_TOKEN! 🔐✨
from datetime import timedelta
from uuid import UUID
from enum import Enum
from src.infrastructure.transcription import WhisperTranscriber
from src.infrastructure.audio import FFmpegAudioProcessor
from src.infrastructure.diarization import PyannoteDiarizer
from src.infrastructure.logging import LocalLogger # 📄✨
from src.application.pipeline import AudioProcessingPipeline
from src.application.services import MaxOverlapAlignmentService
from src.application.enrichers import SentenceSegmentationEnricher
from src.domain.value_objects import DiarizationOptions

def main():
    # 1. Setup Infrastructure
    logger = LocalLogger(log_file="pipeline_debug.log")
    audio_processor = FFmpegAudioProcessor(logger=logger.get_child("AudioProcessor"))
    transcriber = WhisperTranscriber(
        executable_path="/home/user/Documents/GitHub/whisper.cpp/build/bin/whisper-cli",
        model_path="/home/user/Documents/GitHub/whisper.cpp/models/ggml-large-v3.bin",
        logger=logger.get_child("Transcriber")
    )
    diarizer = PyannoteDiarizer(logger=logger.get_child("Diarizer"))
    
    # 2. Setup Application
    enrichers = [
        SentenceSegmentationEnricher(max_duration_seconds=15.0)
    ]
    
    pipeline = AudioProcessingPipeline(
        audio_processor=audio_processor,
        transcriber=transcriber,
        diarizer=diarizer,
        alignment_service=MaxOverlapAlignmentService(),
        logger=logger.get_child("Orchestrator"),
        enrichers=enrichers
    )
    
    # 3. Execute
    logger.info("🚀 Starting SOTA Audio Pipeline...")
    source = "tests/data/test_30s.m4a"
    options = DiarizationOptions(num_speakers=2)
    job = pipeline.execute(source, "de", diarization_options=options)
    
    if job.error_message:
        print(f"❌ Job failed! {job.error_message}")
    else:
        print(f"✨ Job {job.id} completed successfully with {len(job.utterances)} utterances!")
        
        # Save to JSON for our POC! 📄✨
        import json
        from src.domain.value_objects import TimestampRange, Word, Utterance
        
        def serialize_job(obj):
            if isinstance(obj, (Utterance, Word, TimestampRange)):
                return obj.__dict__
            if isinstance(obj, timedelta):
                return obj.total_seconds()
            if isinstance(obj, UUID):
                return str(obj)
            if isinstance(obj, Enum):
                return obj.name
            return str(obj)

        output_json = f"tests/data/job_result.json"
        with open(output_json, "w", encoding="utf-8") as f:
            # We only care about the utterances for the POC UI
            json.dump({
                "id": str(job.id),
                "utterances": [
                    {
                        "speaker_id": u.speaker_id,
                        "text": u.text,
                        "start": u.timestamp.start.total_seconds(),
                        "end": u.timestamp.end.total_seconds(),
                        "words": [
                            {
                                "text": w.text,
                                "start": w.timestamp.start.total_seconds(),
                                "end": w.timestamp.end.total_seconds()
                            } for w in u.words
                        ]
                    } for u in job.utterances
                ]
            }, f, indent=4)
        
        print(f"💾 Results saved to {output_json} for POC usage! 💎")

if __name__ == "__main__":
    main()
