from abc import ABC, abstractmethod
from typing import List, Callable, Type, TypeVar, Any
from src.domain.value_objects import (
    Utterance,
    LanguageTag,
    DiarizationOptions,
    AudioTranscript,
)
from src.domain.entities import AudioArtifact
from src.domain.events import DomainEvent

T = TypeVar("T", bound=DomainEvent)


class IEventBus(ABC):
    @abstractmethod
    def publish(self, event: DomainEvent):
        pass

    @abstractmethod
    def subscribe(self, event_type: Type[T], handler: Callable[[T], Any]):
        pass


class IResultRepository(ABC):
    """Contract for persisting the final results. Cloud-ready! ☁️📦✨"""

    @abstractmethod
    def save(self, transcript: AudioTranscript, output_path: str):
        pass


class ITranscriptSerializer(ABC):
    """Contract for converting AudioTranscripts into transportable formats. 💎✨"""

    @abstractmethod
    def serialize(self, transcript: AudioTranscript) -> str:
        pass


class ITranscriber(ABC):
    @abstractmethod
    def transcribe(
        self, audio: AudioArtifact, language: LanguageTag
    ) -> List[Utterance]:
        pass


class IDiarizer(ABC):
    @abstractmethod
    def diarize(
        self, audio: AudioArtifact, options: DiarizationOptions = None
    ) -> List[Utterance]:
        pass


class IAlignmentService(ABC):
    @abstractmethod
    def align(
        self, transcription: List[Utterance], diarization: List[Utterance]
    ) -> List[Utterance]:
        pass


class IAudioEnricher(ABC):
    @abstractmethod
    def enrich(
        self, utterances: List[Utterance], language: LanguageTag
    ) -> List[Utterance]:
        pass


class IAudioProcessor(ABC):
    @abstractmethod
    def normalize(self, source_path: str) -> AudioArtifact:
        pass


class ITranslator(ABC):
    @abstractmethod
    def translate(
        self,
        texts: List[str],
        source_lang: LanguageTag,
        target_lang: LanguageTag,
        context: List[str] = None,
    ) -> List[str]:
        pass


class ILogger(ABC):
    @abstractmethod
    def info(self, message: str):
        pass

    @abstractmethod
    def debug(self, message: str):
        pass

    @abstractmethod
    def warning(self, message: str):
        pass

    @abstractmethod
    def error(self, message: str):
        pass
