import json
from src.domain.value_objects import AudioTranscript, Utterance, Word


class JsonTranscriptSerializer:
    """
    Infrastructure service to project our Domain objects into JSON. 📄✨💎
    Keeps the Domain pure and follows SRP to the letter! 🏛️⚖️
    """

    def serialize(self, transcript: AudioTranscript) -> str:
        """Converts an AudioTranscript into a high-fidelity JSON string. Projecting... 🏹🎯"""
        data = {
            "target_language": transcript.target_language,
            "utterances": [self._utterance_to_dict(u) for u in transcript.utterances]
        }
        return json.dumps(data, indent=4, ensure_ascii=False)

    def _utterance_to_dict(self, u: Utterance) -> dict:
        return {
            "speaker_id": u.speaker_id,
            "text": u.text,
            "translated_text": u.translated_text,
            "start": u.timestamp.start.total_seconds(),
            "end": u.timestamp.end.total_seconds(),
            "confidence": float(u.confidence),
            "words": [self._word_to_dict(w) for w in u.words],
        }

    def _word_to_dict(self, w: Word) -> dict:
        return {
            "text": w.text,
            "start": w.timestamp.start.total_seconds(),
            "end": w.timestamp.end.total_seconds(),
            "confidence": float(w.confidence),
        }
