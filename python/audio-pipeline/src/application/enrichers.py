import re
from typing import List
from src.domain.interfaces import IAudioEnricher
from src.domain.value_objects import Utterance, LanguageTag, TimestampRange, Word

class SentenceSegmentationEnricher(IAudioEnricher):
    """
    Splits long monologues into manageable rows that end with a complete sentence.
    
    This enricher ensures that:
    1. Each row is a complete sentence (or multiple sentences).
    2. Rows don't exceed a 'max_duration' unless a single sentence is longer than that.
    
    Language Dependency:
    - Uses punctuation regex optimized for Latin-based languages (German, English, etc.) 
      using '.', '!', '?'.
    """
    
    def __init__(self, max_duration_seconds: float = 15.0):
        self.max_duration_seconds = max_duration_seconds
        self.sentence_end_regex = re.compile(r'.*[.!?]$')

    def enrich(self, utterances: List[Utterance], language: LanguageTag) -> List[Utterance]:
        if not utterances:
            return []

        enriched_utterances = []
        
        for utterance in utterances:
            duration = (utterance.timestamp.end - utterance.timestamp.start).total_seconds()
            
            if duration <= self.max_duration_seconds:
                enriched_utterances.append(utterance)
                continue
            
            if not utterance.words:
                enriched_utterances.append(utterance) 
                continue
                
            current_split_words: List[Word] = []
            
            for word in utterance.words:
                current_split_words.append(word)
                
                is_sentence_end = self.sentence_end_regex.match(word.text.strip())
                slice_duration = (word.timestamp.end - current_split_words[0].timestamp.start).total_seconds()
                
                if is_sentence_end and slice_duration >= self.max_duration_seconds:
                    enriched_utterances.append(self._create_utterance_from_words(current_split_words, utterance.speaker_id))
                    current_split_words = []
            
            if current_split_words:
                enriched_utterances.append(self._create_utterance_from_words(current_split_words, utterance.speaker_id))
                
        return enriched_utterances

    def _create_utterance_from_words(self, words: List[Word], speaker_id: str) -> Utterance:
        text = " ".join([w.text for w in words]).strip()
        start = words[0].timestamp.start
        end = words[-1].timestamp.end
        avg_confidence = sum(w.confidence for w in words) / len(words) if words else 1.0
        
        return Utterance(
            timestamp=TimestampRange(start=start, end=end),
            text=text,
            speaker_id=speaker_id,
            confidence=avg_confidence,
            words=words
        )
