import os
import torch
from typing import List
from datetime import timedelta
from pyannote.audio import Pipeline
from src.domain.interfaces import IDiarizer, ILogger, NullLogger
from src.domain.entities import AudioArtifact
from src.domain.value_objects import Utterance, TimestampRange, ConfidenceScore, DiarizationOptions

class PyannoteDiarizer(IDiarizer):
    def __init__(self, logger: ILogger = NullLogger()):
        self.logger = logger
        self.pipeline = None
        self._initialize_pipeline()

    def _initialize_pipeline(self):
        token = os.environ.get("HF_TOKEN")
        if not token:
            raise ValueError("❌ Missing HF_TOKEN! Pyannote needs a token to load gated SOTA models!")

        try:
            self.logger.debug("Loading SOTA Pyannote community-1 diarization pipeline... 🚀✨")
            device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
            # Upgrade to the community-1 model for VBx clustering goodness! 🧩💎
            self.pipeline = Pipeline.from_pretrained(
                "pyannote/speaker-diarization-community-1", 
                token=token
            )
            if self.pipeline:
                self.pipeline.to(device)
                self.logger.debug(f"Pyannote 4.0 community-1 pipeline loaded on {device}!")
            else:
                raise RuntimeError("Diarizer pipeline failed to load! 😱")
        except Exception as e:
            self.logger.error(f"Failed to load Pyannote pipeline: {str(e)}")
            raise

    def diarize(self, audio: AudioArtifact, options: DiarizationOptions = None) -> List[Utterance]:
        """
        Runs the SOTA Pyannote 4.0 community-1 pipeline to find speaker turns. 🕵️‍♀️🏷️
        """
        if not self.pipeline:
            raise RuntimeError("Diarizer pipeline not initialized!")

        self.logger.debug(f"Running diarization on {audio.file_path} with options: {options}")
        
        # Prepare kwargs for the pipeline
        kwargs = {}
        if options:
            if options.num_speakers is not None:
                kwargs["num_speakers"] = options.num_speakers
            if options.min_speakers is not None:
                kwargs["min_speakers"] = options.min_speakers
            if options.max_speakers is not None:
                kwargs["max_speakers"] = options.max_speakers

        # Run pipeline with dynamic options! 🚀✨
        output = self.pipeline(audio.file_path, **kwargs)
        
        # In 4.0+, the pipeline returns a DiarizeOutput wrapper 📄✨
        diarization = output.speaker_diarization
        
        turns = []
        for segment, _, speaker in diarization.itertracks(yield_label=True):
            turns.append(Utterance(
                timestamp=TimestampRange(
                    start=timedelta(seconds=segment.start),
                    end=timedelta(seconds=segment.end)
                ),
                text="", 
                speaker_id=speaker,
                confidence=ConfidenceScore(1.0)
            ))
            
        self.logger.debug(f"Diarization complete! Found {len(turns)} speaker turns.")
        return turns
