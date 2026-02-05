import pytest
from src.infrastructure.mappers import WhisperOutputMapper, AzureTranscriptionMapper
from datetime import timedelta


class TestWhisperOutputMapper:
    def test_map_empty_data(self):
        mapper = WhisperOutputMapper()
        assert mapper.map({}) == []

    def test_map_valid_data(self):
        data = {
            "transcription": [
                {
                    "offsets": {"from": 0, "to": 1000},
                    "tokens": [
                        {
                            "text": "Hello",
                            "offsets": {"from": 0, "to": 500},
                            "p": 0.9,
                        },
                        {
                            "text": " world",
                            "offsets": {"from": 500, "to": 1000},
                            "p": 0.8,
                        },
                        {
                            "text": "!",  # Sub-word token (no space) 🧩
                            "offsets": {"from": 1000, "to": 1100},
                            "p": 0.7,
                        },
                    ],
                    "text": "Hello world!",
                }
            ]
        }
        mapper = WhisperOutputMapper()
        utterances = mapper.map(data)

        assert len(utterances) == 1
        utterance = utterances[0]
        assert utterance.text == "Hello world!"
        assert len(utterance.words) == 2
        assert utterance.words[0].text == "Hello"
        assert utterance.words[1].text == "world!"  # Merged! 🧼
        assert utterance.words[1].timestamp.end == timedelta(milliseconds=1100)



class TestAzureTranscriptionMapper:
    def test_map_empty_data(self):
        mapper = AzureTranscriptionMapper()
        assert mapper.map({}) == []

    def test_map_valid_data(self):
        data = {
            "phrases": [
                {
                    "offsetMilliseconds": 0,
                    "durationMilliseconds": 1000,
                    "speaker": 1,
                    "words": [
                        {
                            "text": "Hello",
                            "offsetMilliseconds": 0,
                            "durationMilliseconds": 500,
                            "confidence": 0.9,
                        },
                        {
                            "text": "world",
                            "offsetMilliseconds": 500,
                            "durationMilliseconds": 500,
                            "confidence": 0.8,
                        },
                    ],
                    "text": "Hello world",
                    "confidence": 0.95,
                }
            ]
        }
        mapper = AzureTranscriptionMapper()
        utterances = mapper.map(data)

        assert len(utterances) == 1
        utterance = utterances[0]
        assert utterance.text == "Hello world"
        assert utterance.speaker_id == "1"
        assert utterance.timestamp.start == timedelta(milliseconds=0)
        assert utterance.timestamp.end == timedelta(milliseconds=1000)
        assert len(utterance.words) == 2
        assert utterance.words[0].text == "Hello"
        assert utterance.words[0].confidence == 0.9
        assert utterance.words[1].text == "world"
        assert utterance.words[1].confidence == 0.8
