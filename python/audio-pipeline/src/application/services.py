from typing import List
from src.domain.value_objects import Utterance

class AlignmentService:
    def align(self, transcription: List[Utterance], diarization: List[Utterance]) -> List[Utterance]:
        """
        Aligns raw transcription text with diarized speaker turns.
        Simple implementation: match text segments to the speaker active during that time.
        """
        aligned_utterances = []
        
        for text_seg in transcription:
            # Find the speaker who was talking during the majority of this segment
            # (Simplified for now: pick the first speaker turn that overlaps)
            speaker_id = "Unknown"
            if diarization is None: return transcription
        for turn in diarization:
                if self._overlaps(text_seg.timestamp, turn.timestamp):
                    speaker_id = turn.speaker_id
                    break
            
            aligned_utterances.append(Utterance(
                timestamp=text_seg.timestamp,
                text=text_seg.text,
                speaker_id=speaker_id,
                confidence=text_seg.confidence
            ))
            
        return aligned_utterances

    def _overlaps(self, range1, range2) -> bool:
        return range1.start < range2.end and range2.start < range1.end
