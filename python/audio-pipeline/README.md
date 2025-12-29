# 🎙️ Audio Pipeline

An audio processing pipeline for language learning applications. Built using Domain-Driven Design (DDD) and Clean Architecture principles.

## 🏗️ Architecture
The project follows a layered approach:
- **Domain**: Business logic and value objects (`Utterance`, `Word`, `AudioTranscript`).
- **Application**: Orchestration logic and service interfaces.
- **Infrastructure**: Implementation adapters for Whisper, Pyannote, Llama 3.1, and Azure AI Services.

## ✨ Features
- **Transcription**: Word-level timestamps for audio alignment.
- **Diarization**: Speaker identification and turn detection.
- **Alignment**: Logic to associate text with specific speakers.
- **Segmentation**: Monologue splitting for improved readability.
- **Translation**: Context-aware German-to-English translation using local LLMs (Llama 3.1) or Azure AI Foundry.

## 🚀 Getting Started

### Prerequisites
- Python 3.13+ (Managed via `uv`)
- `ffmpeg` installed and available in your system PATH.
- `llama.cpp` built and available (for local translation).
- A HuggingFace account and `HF_TOKEN` configured for accessing gated models.

### ⚙️ Configuration
Copy the `.env.example` file to `.env` and fill in your credentials:
```bash
cp .env.example .env
```

| Variable | Required For | Description |
|----------|--------------|-------------|
| `HF_TOKEN` | Local Stack | Access to Pyannote diarization models. |
| `AZURE_SPEECH_KEY` | Azure Stack | API Key for Azure AI Speech (Fast Transcription). |
| `AZURE_SPEECH_REGION` | Azure Stack | Region for the Speech resource (e.g., `eastus2`). |
| `AZURE_AI_INFERENCE_KEY` | Azure Stack | API Key for Azure AI Foundry Inference. |
| `AZURE_AI_INFERENCE_ENDPOINT` | Azure Stack | Full endpoint URL for the Foundry model. |

### 🧠 Local Models
If running in local mode (default), download the following:
1. Create the `models/` directory: `mkdir -p python/audio-pipeline/models`
2. Download **Llama 3.1 8B Instruct (Q4_K_M)**:
   - [HuggingFace - bartowski/Meta-Llama-3.1-8B-Instruct-GGUF](https://huggingface.co/bartowski/Meta-Llama-3.1-8B-Instruct-GGUF)
   - Place the file at: `python/audio-pipeline/models/llama-3.1-8b-instruct-q4_k_m.gguf`

### Usage
Run the pipeline using `uv`:
```bash
# Local Mode (Whisper + Pyannote + Llama.cpp)
uv run main.py <path_to_audio> --output-dir ./output --language de

# Azure Mode (Cloud-Native Transcription & Foundry Translation)
uv run main.py <path_to_audio> --output-dir ./output --language de --use-azure
```

## 🛠️ Developer Tools
Located in the `tools/` directory:
- **Pipeline Explorer (`poc_ui.html`)**: A tool to visualize the processed output.
- **Comparison Tool (`compare_results.py`)**: Checks output against labels.
- **Verification Helper (`create_verification_json.py`)**: Generates templates for data verification.
