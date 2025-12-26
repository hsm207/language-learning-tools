from abc import ABC, abstractmethod
from typing import List
from src.domain.value_objects import Utterance, LanguageTag
from src.domain.entities import AudioArtifact

class ITranscriber(ABC):
    @abstractmethod
    def transcribe(self, audio: AudioArtifact, language: LanguageTag) -> List[Utterance]:
        pass

class IDiarizer(ABC):
    @abstractmethod
    def diarize(self, audio: AudioArtifact) -> List[Utterance]:
        pass

class IAudioEnricher(ABC):
    @abstractmethod
    def enrich(self, utterances: List[Utterance], language: LanguageTag) -> List[Utterance]:
        pass

class IAudioProcessor(ABC):
    @abstractmethod
    def normalize(self, source_path: str) -> AudioArtifact:
        pass

class ILogger(ABC):
    @abstractmethod
    def info(self, message: str):
        pass

    @abstractmethod
    def debug(self, message: str):
        pass

    @abstractmethod
    def error(self, message: str):
        pass

class NullLogger(ILogger):
    """The silent treatment, but for code! 🤫💖"""
    def info(self, message: str): pass
    def debug(self, message: str): pass
    def error(self, message: str): pass
