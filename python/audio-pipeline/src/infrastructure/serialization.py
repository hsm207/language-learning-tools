import json
from datetime import timedelta
from typing import Dict, Any
from src.domain.value_objects import (
    AudioTranscript,
    Utterance,
    Word,
    TimestampRange,
    ConfidenceScore,
)
from src.domain.interfaces import ITranscriptSerializer


class JsonTranscriptSerializer(ITranscriptSerializer):
    """
    Handles serialization of AudioTranscript to/from JSON. 💎✨
    """

    def serialize(self, transcript: AudioTranscript) -> str:
        data = {
            "target_language": transcript.target_language,
            "total_duration": transcript.total_duration.total_seconds(),
            "utterances": [self._utterance_to_dict(u) for u in transcript.utterances],
        }
        return json.dumps(data, indent=2, ensure_ascii=False)

    def _utterance_to_dict(self, u: Utterance) -> Dict[str, Any]:
        data = {
            "start": u.timestamp.start.total_seconds(),
            "end": u.timestamp.end.total_seconds(),
            "speaker": u.speaker_id,
            "text": u.text,
            "confidence": float(u.confidence),
            "words": [self._word_to_dict(w) for w in u.words],
        }
        # Flatly merge metadata for simpler consumption by consumers! 🧼💎
        data.update(u.metadata)
        return data

    def _word_to_dict(self, w: Word) -> Dict[str, Any]:
        return {
            "start": w.timestamp.start.total_seconds(),
            "end": w.timestamp.end.total_seconds(),
            "text": w.text,
            "confidence": float(w.confidence),
        }
