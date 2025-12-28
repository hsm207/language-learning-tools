from src.domain.interfaces import IResultRepository, ITranscriptSerializer
from src.domain.value_objects import AudioTranscript


class FileSystemResultRepository(IResultRepository):
    """
    Local filesystem implementation of IResultRepository.
    Encapsulates disk I/O, keeping the Application layer clean and cloud-ready! 📁🛡️⚖️
    """

    def __init__(self, serializer: ITranscriptSerializer):
        self.serializer = serializer

    def save(self, transcript: AudioTranscript, output_path: str):
        """Saves the SOTA transcript to the specified local path. 💾💎"""
        content = self.serializer.serialize(transcript)
        with open(output_path, "w", encoding="utf-8") as f:
            f.write(content)
