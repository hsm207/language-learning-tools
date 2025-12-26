# 🎙️ Audio Pipeline

An audio processing pipeline for language learning applications. Built using Domain-Driven Design (DDD) and Clean Architecture principles.

## 🏗️ Architecture
The project follows a layered approach:
- **Domain**: Business logic and value objects (`Utterance`, `Word`, `AudioTranscript`).
- **Application**: Orchestration logic and service interfaces.
- **Infrastructure**: Implementation adapters for Whisper, Pyannote, and FFmpeg.

## ✨ Features
- **Transcription**: Word-level timestamps for audio alignment.
- **Diarization**: Speaker identification and turn detection.
- **Alignment**: Logic to associate text with specific speakers.
- **Segmentation**: Monologue splitting for improved readability.

## 🚀 Getting Started

### Prerequisites
- Python 3.10+
- `ffmpeg` installed and available in your system PATH.
- A HuggingFace account and `HF_TOKEN` configured in a `.env` file for accessing gated models.

### Usage
Process an audio file via the CLI:
```bash
python3 main.py <path_to_audio> --output-dir ./output --language de --num-speakers 2
```

The output directory will contain:
- `transcript.json`: The structured output.
- `pipeline.log`: Execution logs.
- `temp/`: Intermediary artifacts preserved for verification.

## 🛠️ Developer Tools
Located in the `tools/` directory:
- **Pipeline Explorer (`poc_ui.html`)**: A tool to visualize the processed output.
- **Comparison Tool (`compare_results.py`)**: Checks output against labels.
- **Verification Helper (`create_verification_json.py`)**: Generates templates for data verification.