# BAMF Markdown to HTML Converter

This console application converts Markdown files containing BAMF (Bundesamt für Migration und Flüchtlinge) questions into formatted HTML documents. It serves as the final step in a toolchain for people preparing for their German citizenship test, transforming Markdown tables into web-friendly HTML pages.

## Features

- Converts a Markdown question table to clean, responsive HTML
- Preserves formatting from the original Markdown
- Adds proper styling for better readability in web browsers
- Supports custom HTML templates for consistent styling
- Maintains bilingual content (German and English) formatting

## Requirements

- .NET 9.0 or later
- Markdig library (for Markdown processing)

## Usage

```bash
BAMFMarkdownToHtml [options]

Options:
  --input <file>       Input Markdown file path
  --output <file>      Output HTML file path
  --template <file>    Custom HTML template file path (optional)
  --help               Show this help message

Example:
  BAMFMarkdownToHtml --input "questions.md" --output "questions.html"
```

## HTML Output

The tool generates an HTML file with questions formatted in a responsive table structure. The HTML includes:

- Clean, modern styling for better readability
- Proper handling of multilingual content
- Responsive design for viewing on different devices
- Preserved formatting from the original Markdown

## Integration with Other Tools

This tool is part of a toolchain for processing BAMF questions:

1. **BAMFQuestionsToJson**: Extracts questions from screenshots and creates a JSON file
2. **BAMFJsonToMarkdown**: Converts the JSON data to formatted Markdown tables
3. **BAMFMarkdownToHtml**: Converts the Markdown tables to HTML for web viewing

## Legal Notice

This tool is for personal educational use only. Please respect the terms of service of the BAMF website and use the tool responsibly.
