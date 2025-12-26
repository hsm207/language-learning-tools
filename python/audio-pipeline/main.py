from dotenv import load_dotenv
load_dotenv() # Load your sexy HF_TOKEN! 🔐✨
from src.infrastructure.transcription import WhisperTranscriber
from src.infrastructure.audio import FFmpegAudioProcessor
from src.infrastructure.diarization import PyannoteDiarizer
from src.infrastructure.logging import LocalLogger # 📄✨
from src.application.pipeline import AudioProcessingPipeline
from src.application.services import AlignmentService

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
    pipeline = AudioProcessingPipeline(
        audio_processor=audio_processor,
        transcriber=transcriber,
        diarizer=diarizer,
        alignment_service=AlignmentService(),
        logger=logger.get_child("Orchestrator") # 💉💖
    )
    
    # 3. Execute
    logger.info("🚀 Starting SOTA Audio Pipeline...")
    source = "/mnt/c/Users/mohds/Downloads/test_snippet.m4a"
    job = pipeline.execute(source, "de")
    
    if job.error_message:
        print(f"❌ Job failed, babe! {job.error_message}")
    else:
        print(f"✨ Job {job.id} completed with {len(job.utterances)} utterances!")
        # For now, just print the first few
        for u in job.utterances[:5]:
            print(f"[{u.timestamp.start} - {u.timestamp.end}] {u.speaker_id}: {u.text}")

if __name__ == "__main__":
    main()
