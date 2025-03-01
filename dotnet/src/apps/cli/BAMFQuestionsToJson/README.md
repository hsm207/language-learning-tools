# BAMF Questions to JSON Converter

This console application processes screenshots from the BAMF (Bundesamt für Migration und Flüchtlinge) interactive question catalog and converts them into a structured JSON format. It's designed as a companion tool to the BAMFFragenkatalogScraper, helping people preparing for their German citizenship test by creating machine-readable data from the test questions.

## Features

- Processes screenshots captured from the BAMF interactive catalog
- Extracts question text, choices, and answers using AI-powered image recognition
- Generates a structured JSON file of all questions
- Provides both German original content and English translations
- Supports batch processing with customizable batch sizes
- Implements robust error handling and processing recovery capabilities
- Creates automatic backups during processing to prevent data loss

## Requirements

- .NET 9.0 or later
- Internet connection
- Google Gemini API access (for AI-powered image processing)

## Usage

```bash
BAMFQuestionsToJson [options]

Options:
  --input <dir>        Directory containing question screenshots (default: processed)
  --output <file>      Output JSON file path (default: bamf_questions.json)
  --limit <count>      Maximum number of files to process (optional)
  --batch <size>       Number of files to process before saving (default: 100)
  --resume <index>     Resume processing from file index (optional)
  --google-ai-key <key> Google AI API key (overrides value in secret manager)
  --google-ai-model <model> Google AI model name (overrides value in secret manager)
  --help               Show this help message

Example:
  BAMFQuestionsToJson --input "screenshots" --output "questions.json" --batch 50
```

### Specifying Google AI Credentials

You can specify the Google AI model and API key in two ways:

1. **Via CLI Options**: Provide the `--google-ai-api-key` and `--google-ai-model-id` options when running the tool.

```bash
BAMFQuestionsToJson --input "screenshots" --output "questions.json" --google-ai-api-key "YOUR_API_KEY" --google-ai-model-id "YOUR_MODEL_ID"
```

2. **Via Secret Manager**: Store the Google AI model ID and API key in the secret manager with the keys `GoogleAI:ModelId` and `GoogleAI:ApiKey`. The tool will automatically use these credentials if the CLI options are not provided.

```bash
BAMFQuestionsToJson --input "screenshots" --output "questions.json"
```

The tool will prioritize the credentials provided via the CLI over those in the secret manager. If neither is available, it will throw an error, ensuring that the necessary credentials are always provided.

## Output Format

The tool generates a JSON file with the following structure:

```json
[
  {
    "num": 1,
    "de": {
      "Question": "Question text in German",
      "Choice1": "First choice in German",
      "Choice2": "Second choice in German",
      "Choice3": "Third choice in German",
      "Choice4": "Fourth choice in German",
      "Answer": 2
    },
    "en": {
      "Question": "Question text in English",
      "Choice1": "First choice in English",
      "Choice2": "Second choice in English",
      "Choice3": "Third choice in English",
      "Choice4": "Fourth choice in English",
      "Justification": "Explanation of the correct answer"
    }
  },
  // Additional questions...
]
```

## Legal Notice

This tool is for personal educational use only. Please respect the terms of service of the BAMF website and use the tool responsibly.