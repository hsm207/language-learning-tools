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
  --help               Show this help message

Example:
  BAMFQuestionsToJson --input "screenshots" --output "questions.json" --batch 50
```

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