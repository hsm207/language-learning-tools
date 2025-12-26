from typing import List
from src.domain.interfaces import IAlignmentService
from src.domain.value_objects import Utterance

class MaxOverlapAlignmentService(IAlignmentService):
    def align(self, transcription: List[Utterance], diarization: List[Utterance]) -> List[Utterance]:
        """
        Aligns raw transcription text with diarized speaker turns using weighted max overlap. ⚖️🎯
        """
        if not diarization:
            return transcription

        aligned_utterances = []
        for text_seg in transcription:
            best_speaker = "Unknown"
            max_overlap = 0.0
            
            for turn in diarization:
                overlap = self._get_overlap_duration(text_seg.timestamp, turn.timestamp)
                if overlap > max_overlap:
                    max_overlap = overlap
                    best_speaker = turn.speaker_id
            
            aligned_utterances.append(Utterance(
                timestamp=text_seg.timestamp,
                text=text_seg.text,
                speaker_id=best_speaker,
                confidence=text_seg.confidence,
                words=text_seg.words
            ))
            
        return aligned_utterances

    def _get_overlap_duration(self, range1, range2) -> float:
        """Calculates the duration of overlap between two timestamp ranges in seconds. 📏✨"""
        start = max(range1.start, range2.start)
        end = min(range1.end, range2.end)
        return max(0.0, (end - start).total_seconds())

    def _overlaps(self, range1, range2) -> bool:
        return range1.start < range2.end and range2.start < range1.end
