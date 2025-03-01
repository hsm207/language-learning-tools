# BAMF JSON to Markdown Converter

This console application converts JSON files containing BAMF (Bundesamt für Migration und Flüchtlinge) question data into formatted Markdown tables. It's designed to work as part of a toolchain for people preparing for their German citizenship test, transforming structured JSON data into human-readable Markdown format.

## Features

- Converts a list of structured JSON question data to a Markdown table
- Preserves both German original content and English translations
- Formats questions, choices, answers, and justifications in an easy-to-read table layout
- Handles proper escaping of special characters for Markdown compatibility
- Sorts questions by question number for consistent presentation

## Requirements

- .NET 9.0 or later
- Markdig library (for Markdown processing)

## Usage

```bash
BAMFJsonToMarkdown [options]

Options:
  --input <file>       Input JSON file path
  --output <file>      Output Markdown file path
  --help               Show this help message

Example:
  BAMFJsonToMarkdown --input "bamf_questions.json" --output "questions.md"
```

## Output Format

The tool generates a Markdown file with questions formatted as a table:

```markdown
| Num | Question | Choices | Answer | Justification |
|-----|----------|---------|--------|---------------|
| 1 | Question text in German<br><br>(Question text in English) | 1. First choice in German<br>   (First choice in English)<br><br>2. Second choice in German<br>   (Second choice in English)<br><br>3. Third choice in German<br>   (Third choice in English)<br><br>4. Fourth choice in German<br>   (Fourth choice in English) | 2 | Explanation of the correct answer |
```

## Integration with Other Tools

This tool is part of a toolchain for processing BAMF questions:

1. **BAMFQuestionsToJson**: Extracts questions from screenshots and creates a JSON file
2. **BAMFJsonToMarkdown**: Converts the JSON data to formatted Markdown tables
3. **BAMFMarkdownToHtml**: Converts the Markdown tables to HTML for web viewing

## Legal Notice

This tool is for personal educational use only. Please respect the terms of service of the BAMF website and use the tool responsibly.
