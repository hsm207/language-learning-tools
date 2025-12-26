import os
import torch
from typing import List
from datetime import timedelta
from pyannote.audio import Pipeline
from src.domain.interfaces import IDiarizer, ILogger, NullLogger
from src.domain.entities import AudioArtifact
from src.domain.value_objects import Utterance, TimestampRange, ConfidenceScore

class PyannoteDiarizer(IDiarizer):
    def __init__(self, logger: ILogger = NullLogger()):
        self.logger = logger
        self.pipeline = None
        self._initialize_pipeline()

    def _initialize_pipeline(self):
        token = os.environ.get("HF_TOKEN")
        if not token:
            raise ValueError("❌ Missing HF_TOKEN! Pyannote needs a token to load gated SOTA models, honey! 💋")

        try:
            self.logger.debug("Loading SOTA Pyannote 4.0.3 diarization pipeline...")
            device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
            # We use the pre-trained 3.1 pipeline name which resolves to the latest SOTA bits
            self.pipeline = Pipeline.from_pretrained(
                "pyannote/speaker-diarization-3.1", 
                token=token
            )
            if self.pipeline:
                self.pipeline.to(device)
                self.logger.debug(f"Pyannote pipeline loaded on {device}!")
            else:
                raise RuntimeError("Diarizer pipeline failed to load, babe! 😱")
        except Exception as e:
            self.logger.error(f"Failed to load Pyannote pipeline: {str(e)}")
            raise

    def diarize(self, audio: AudioArtifact) -> List[Utterance]:
        """
        Runs the SOTA Pyannote 4.0.3 pipeline to find speaker turns. 🕵️‍♀️🏷️
        """
        if not self.pipeline:
            raise RuntimeError("Diarizer pipeline not initialized, honey! 💋")

        self.logger.debug(f"Running diarization on {audio.file_path}...")
        
        # Pyannote returns a DiarizeOutput in 4.0
        output = self.pipeline(audio.file_path)
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
