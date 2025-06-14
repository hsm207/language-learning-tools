# SubtitleTranslator CLI

A command-line tool for translating subtitle files using Google Gemini API. We built this for language learning - it keeps all the timing info while translating subtitles, so you can create bilingual learning materials.

## 🚀 What It Does

Takes subtitle files (TTML2, SRT, VTT) and translates them from one language to another using Google Gemini API. Spits out a JSON file with both the original and translated text plus all the timing data - handy for language learning apps and tools.

### What's Cool About It

- **Multiple subtitle formats**: Works with TTML2, SRT, and VTT files
- **Smart context handling**: Uses a rolling window approach so translations make sense across scenes
- **Respects rate limits**: Won't spam the API - defaults to 8 requests per minute
- **Actually handles errors**: Retries failed requests instead of just dying
- **Verbose logging**: See what's happening under the hood if you want
- **Clean output**: JSON format that's easy to work with in other tools

## 🏗️ How It Works

### The Basic Idea

```
Subtitle File → Parser → Batcher → Google Gemini API → JSON Output
     ↓            ↓        ↓              ↓                ↓
   .ttml        Domain   Rolling    Context-Aware     Translated
   .ttml2       Models   Window     Translation       Subtitles
   .srt                  Batches    
   .vtt
```

### What Actually Happens

1. **Parsing**: Reads your subtitle file and extracts the text + timing info
2. **Batching**: Groups subtitles into chunks of ~15 lines with some overlap so the AI has context
3. **Translation**: Sends each batch to Google Gemini API with a prompt that includes context from previous lines
4. **Rate Limiting**: Waits between API calls so we don't get banned
5. **Output**: Writes everything to a JSON file with original text, translation, and timing

### What You Need

- **.NET 9.0+**: The runtime this thing is built on
- **Microsoft Semantic Kernel**: Handles the AI orchestration 
- **Google Gemini API**: The actual translation happens here
- **Polly**: Makes the HTTP calls more reliable with retries
- **System.CommandLine**: Modern CLI framework for .NET

## 📦 Getting Started

### What You Need First

1. **.NET 9 SDK** on your machine
2. **Google Gemini API key** - grab one from [Google AI Studio](https://aistudio.google.com/app/apikey) (it's free for basic usage)

### Set Up Your API Key

```bash
# macOS/Linux
export GEMINI_API_KEY="your-api-key-here"

# Windows
set GEMINI_API_KEY=your-api-key-here
```

## 🎯 Usage

### Basic Usage

```bash
dotnet run --project SubtitleTranslator.csproj -- \
  --input path/to/subtitles.ttml2 \
  --source-language german \
  --target-language english
```

### Full Command Options

```bash
dotnet run --project SubtitleTranslator.csproj -- \
  --input path/to/subtitles.ttml2 \
  --output path/to/translated_output.json \
  --source-language german \
  --target-language english \
  --subtitle-format ttml \
  --api-key your-api-key \
  --requests-per-minute 8 \
  --verbose
```

### Command Line Options

| Option | Required | Description | Default |
|--------|----------|-------------|---------|
| `--input` | ✅ | Path to the subtitle file to translate | - |
| `--output` | ❌ | Path for the translated JSON output | `{input}_translated.json` |
| `--source-language` | ✅ | Source language (e.g., 'german', 'de') | - |
| `--target-language` | ✅ | Target language (e.g., 'english', 'en') | - |
| `--subtitle-format` | ❌ | Input format (ttml2, srt, vtt) | Auto-detected |
| `--api-key` | ❌ | Gemini API key | `GEMINI_API_KEY` env var |
| `--requests-per-minute` | ❌ | Rate limiting (requests per minute) | 8 |
| `--verbose` | ❌ | Enable detailed logging | false |

### Language Codes

Right now we only support German ↔ English translation:

- **German**: `german`, `de`
- **English**: `english`, `en`

(Yeah, just these two for now - we built this for German language learning!)

### Subtitle Files

Right now we've only tested with:

- **TTML2** (.ttml2) - works great! We've processed large files with 500+ lines

We have plans to add support for:
- **SRT** (.srt) 
- **VTT** (.vtt)

But for now, stick with TTML2 files if you want guaranteed results!

## 📊 Performance Stuff

### How Long Will This Take?

- **Small files** (< 100 lines): 1-2 minutes
- **Medium files** (100-500 lines): 3-8 minutes  
- **Large files** (500+ lines): 8+ minutes

### Rate Limiting (aka "Why Is This So Slow?")

We default to **8 requests per minute** because that's what Gemini's free tier allows:
- Each request translates ~15 subtitle lines
- There's a 7.5-second pause between API calls
- If you hit rate limits, it backs off automatically

If you've got a paid API plan, bump up `--requests-per-minute` to go faster.

## 📄 What You Get Out

The JSON output looks like this:

```json
[
  {
    "StartTime": "00:00:01.508",
    "EndTime": "00:00:06.830",
    "OriginalText": "[Hintergrundgeräusche]",
    "TranslatedText": "[Background noises]"
  },
  {
    "StartTime": "00:00:09.850",
    "EndTime": "00:00:12.019",
    "OriginalText": "Ja na klar, so eine Firma verändert sich unentwegt.",
    "TranslatedText": "Yes, of course, a company like this is constantly changing."
  }
]
```

## 🎨 What To Do With Your JSON File

### Looking at Your Translations

So you've got this JSON file with all your translated subtitles... now what? 

We made a simple web viewer to display the data in a table format:

- Tables are sortable (click the headers)
- You can copy whole columns to paste into Excel or other apps
- Has basic search/filter for finding specific lines
- Works on mobile browsers too

**→ [Subtitle JSON Viewer](../../../docs/tools/subtitle-json-viewer.html)**

Open that HTML file in your browser and upload your JSON file.

### How to Use It

1. Get your JSON file from running this CLI
2. Open the viewer link above in any browser
3. Upload your JSON file (drag and drop or click to browse)
4. View your subtitles in a readable table format

The viewer runs entirely in your browser - no data gets sent anywhere.

## 🔧 Development

### Building from Source

```bash
# Clone the repository
git clone <repository-url>
cd language-learning-tools/dotnet

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the CLI
dotnet run --project src/apps/cli/SubtitleTranslator/SubtitleTranslator.csproj
```

### Project Structure

```
SubtitleTranslator/
├── Program.cs              # CLI entry point and configuration
├── SubtitleTranslator.csproj # Project dependencies
└── README.md               # This file

Dependencies:
├── LanguageLearningTools.Application/  # Application services
├── LanguageLearningTools.Domain/       # Core domain models
└── LanguageLearningTools.Infrastructure/ # Gemini AI integration
```

## 📝 License

This project is part of the Language Learning Tools suite. See the main repository for license information.

## 🤝 Contributing

Want to help make this better? Cool! Just keep in mind:

1. We write tests first (TDD style)
2. Add tests for anything new you build
3. Update docs if you change how things work
4. Try to keep the code clean and follow C# conventions

## 🐛 When Things Go Wrong

### Common Issues You Might Hit

**"Option '--input' is required"**
- You forgot to tell it which file to translate

**"API key not found"**
- Set that `GEMINI_API_KEY` environment variable or use `--api-key`

**"Request timeout" or things taking forever**
- Large files just take time (we measured 5+ minutes for 500+ lines)
- Try `--verbose` to see what's happening

**"Translation failed"**
- Check your internet connection
- Make sure your API key works and you have quota left
- Use `--verbose` to see the actual error

### Making It Faster

- Use `--verbose` to see progress on big files
- Bump `--requests-per-minute` if you have a paid API plan
- For huge files (1000+ lines), maybe split them first

---

## 💬 Questions or Feedback?

Got questions? Hit a weird bug? Have ideas for making this better? 

Feel free to [open an issue](https://github.com/mohdshukrihasan/language-learning-tools/issues) - we'd love to hear from you!

Built for fun and language learning! 🌍✨ Hope it's useful for your projects too.
