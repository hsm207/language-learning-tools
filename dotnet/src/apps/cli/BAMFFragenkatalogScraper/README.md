# BAMF Interaktiver Fragenkatalog Scraper

This console application automates the process of capturing screenshots from the official BAMF (Bundesamt für Migration und Flüchtlinge) [interactive question catalog](https://www.bamf.de/DE/Themen/Integration/ZugewanderteTeilnehmende/OnlineTestcenter/online-testcenter-node.html), which includes both the Leben in Deutschland Test and Einbürgerungstest questions. It's designed to help people preparing for their German citizenship test by collecting all test questions for study purposes.

## Features

- Automatically navigates through all 310 test questions
- Takes screenshots of each question
- Configurable for different German federal states (Bundesländer)
- Customizable screenshot output directory
- Polite scraping with built-in delays to respect server resources

## Requirements

- .NET 9.0 or later
- Internet connection
- Microsoft Playwright dependencies (will be installed automatically)

## Usage

```bash
BAMFFragenkatalogScraper [options]

Options:
  --url <url>          Test URL (default: https://oet.bamf.de/ords/oetut/f?p=514:1::::::)
  --bundesland <state> Federal state (default: Berlin)
  --output <dir>       Screenshot output directory (default: screenshots)
  --help              Show this help message

Example:
  BAMFFragenkatalogScraper --bundesland "Hamburg" --output "my-screenshots"
```

## Legal Notice

This tool is for personal educational use only. Please respect the terms of service of the BAMF website and use the tool responsibly.