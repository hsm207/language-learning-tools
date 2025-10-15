# SubtitleTool

A command-line tool for processing and translating subtitle files.

This tool provides functionalities to convert subtitle files into a structured JSON format and to translate them from a source language to a target language using the Google Gemini API.

## Features

*   **Convert**: Converts subtitle files (e.g., TTML2) into a clean JSON format.
*   **Translate**: Translates subtitle files line-by-line, outputting a JSON file containing both the original and translated text.

## Installation

Build the project using the .NET SDK:

```bash
dotnet build
```

## Usage

You can run the tool using `dotnet run --project <path-to-SubtitleTool.csproj>`.

```bash
dotnet run --project ./src/apps/cli/SubtitleTool/SubtitleTool.csproj -- [command] [options]
```

### Commands

The tool has two main commands: `convert` and `translate`.

---

### `convert`

Converts a subtitle file into a JSON representation of the subtitle lines.

```bash
dotnet run --project ./src/apps/cli/SubtitleTool/SubtitleTool.csproj -- convert --input <input-file>
```

#### Options

| Option      | Alias | Description                                                              | Required |
|-------------|-------|--------------------------------------------------------------------------|----------|
| `--input`   |       | The subtitle file to convert (e.g., `.ttml2`).                            | Yes      |
| `--output`  |       | The output JSON file. If omitted, the output is written to stdout.       | No       |
| `--format`  |       | The subtitle format (e.g., `ttml2`). If omitted, it will be auto-detected. | No       |

#### Example

**Input (`captions.ttml2`):**
```xml
<!-- Sample TTML2 file -->
<tt>
  <body>
    <div>
      <p begin="00:00:01.000" end="00:00:03.000">Hello, world.</p>
      <p begin="00:00:04.000" end="00:00:06.000">This is a subtitle.</p>
    </div>
  </body>
</tt>
```

**Command:**
```bash
dotnet run --project ./src/apps/cli/SubtitleTool/SubtitleTool.csproj -- convert --input ./captions.ttml2 --output ./captions.json
```

**Output (`captions.json`):**
```json
[
  {
    "Begin": "00:00:01.000",
    "End": "00:00:03.000",
    "Text": "Hello, world."
  },
  {
    "Begin": "00:00:04.000",
    "End": "00:00:06.000",
    "Text": "This is a subtitle."
  }
]
```

---

### `translate`

Translates a subtitle file from a source language to a target language. The output is a JSON file containing the original text, the translated text, and timestamps.

```bash
dotnet run --project ./src/apps/cli/SubtitleTool/SubtitleTool.csproj -- translate --input <input-file> --source-language <lang> --target-language <lang>
```

#### Options

| Option                  | Description                                                                                              | Required | Default                               |
|-------------------------|----------------------------------------------------------------------------------------------------------|----------|---------------------------------------|
| `--input`               | The subtitle file to translate (e.g., `.ttml2`).                                                          | Yes      |                                       |
| `--output`              | The output JSON file for translated subtitles.                                                           | No       | `<input-filename>_translated.json`    |
| `--source-language`     | Source language code (e.g., `de` for German).                                                            | Yes      |                                       |
| `--target-language`     | Target language code (e.g., `en` for English).                                                           | Yes      |                                       |
| `--api-key`             | Google Gemini API key. Can also be set via the `GEMINI_API_KEY` environment variable.                      | Yes      |                                       |
| `--subtitle-format`     | Input subtitle format (e.g., `ttml2`). Auto-detected if omitted.                                          | No       |                                       |
| `--requests-per-minute` | Maximum requests per minute to avoid rate limiting.                                                      | No       | `8`                                   |
| `--verbose`             | Enable verbose output for debugging.                                                                     | No       | `false`                               |

#### Example

**Input (`captions_de.ttml2`):**
```xml
<!-- Sample TTML2 file in German -->
<tt>
  <body>
    <div>
      <p begin="00:00:01.000" end="00:00:03.000">Hallo, Welt.</p>
      <p begin="00:00:04.000" end="00:00:06.000">Das ist ein Untertitel.</p>
    </div>
  </body>
</tt>
```

**Command:**
```bash
export GEMINI_API_KEY="YOUR_API_KEY_HERE"
dotnet run --project ./src/apps/cli/SubtitleTool/SubtitleTool.csproj -- translate --input ./captions_de.ttml2 --source-language de --target-language en
```

**Output (`captions_de_translated.json`):**
```json
[
  {
    "Original": {
      "Begin": "00:00:01.000",
      "End": "00:00:03.000",
      "Text": "Hallo, Welt."
    },
    "Translated": {
      "Begin": "00:00:01.000",
      "End": "00:00:03.000",
      "Text": "Hello, world."
    }
  },
  {
    "Original": {
      "Begin": "00:00:04.000",
      "End": "00:00:06.000",
      "Text": "Das ist ein Untertitel."
    },
    "Translated": {
      "Begin": "00:00:04.000",
      "End": "00:00:06.000",
      "Text": "This is a subtitle."
    }
  }
]
```

**Visualize Output:** You can view this JSON output in a user-friendly format using the [Subtitle JSON Viewer](../../../../../docs/tools/subtitle-json-viewer.html).
