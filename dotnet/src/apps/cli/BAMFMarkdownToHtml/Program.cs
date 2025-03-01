using Markdig;
using System.CommandLine;
using System.Text.RegularExpressions;

namespace BAMFMarkdownToHtml
{
    class Program
    {
        private const string DarkThemeCSS = @"
<style>
    .dark-table {
        border-collapse: collapse;
        width: 100%;
        margin: 25px 0;
        font-size: 0.9em;
        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
        box-shadow: 0 0 20px rgba(0, 0, 0, 0.15);
        border-radius: 5px;
        overflow: hidden;
    }
    
    .dark-table thead tr {
        background-color: #333;
        color: #ffffff;
        text-align: left;
        font-weight: bold;
    }
    
    .dark-table th,
    .dark-table td {
        padding: 12px 15px;
    }
    
    .dark-table tbody tr {
        border-bottom: 1px solid #444;
        background-color: #222;
        color: #ddd;
    }
    
    .dark-table tbody tr:nth-of-type(even) {
        background-color: #2a2a2a;
    }
    
    .dark-table tbody tr:last-of-type {
        border-bottom: 2px solid #333;
    }
    
    .dark-table tbody tr:hover {
        background-color: #444;
        color: #fff;
    }
</style>
";

        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("Markdown to HTML Table Converter (Dark Theme)");
            Console.WriteLine("============================================");

            var inputOption = new Option<FileInfo?>(
                aliases: new[] { "--input", "-i" },
                description: "The Markdown file to convert");

            var outputOption = new Option<FileInfo?>(
                aliases: new[] { "--output", "-o" },
                description: "The HTML output file");

            var rootCommand = new RootCommand("Converts Markdown tables to HTML with a dark theme");
            rootCommand.AddOption(inputOption);
            rootCommand.AddOption(outputOption);

            rootCommand.SetHandler(async (input, output) =>
            {
                string? markdownInput = null;
                string? outputPath = null;

                // Handle input source (file or console)
                if (input != null && input.Exists)
                {
                    markdownInput = File.ReadAllText(input.FullName);
                }
                else
                {
                    Console.WriteLine("Enter your markdown table text below (Ctrl+D or Ctrl+Z followed by Enter to finish):");
                    markdownInput = ReadMultilineInput();
                }

                if (string.IsNullOrWhiteSpace(markdownInput))
                {
                    Console.WriteLine("No input provided. Exiting.");
                    return;
                }

                // Handle output destination (file or console)
                if (output != null)
                {
                    outputPath = output.FullName;
                }
                else if (input != null)
                {
                    // Default to same name as input but with .html extension
                    outputPath = Path.ChangeExtension(input.FullName, "html");
                }
                else
                {
                    Console.Write("Enter output file path (leave blank for console output): ");
                    outputPath = Console.ReadLine();
                }

                string html = ConvertMarkdownTableToHtml(markdownInput);

                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    Console.WriteLine("\nGenerated HTML:");
                    Console.WriteLine(html);
                }
                else
                {
                    File.WriteAllText(outputPath, html);
                    Console.WriteLine($"HTML has been written to: {outputPath}");
                }
            }, inputOption, outputOption);

            return await rootCommand.InvokeAsync(args);
        }

        private static string ReadMultilineInput()
        {
            var lines = new List<string>();
            string? line;
            
            while ((line = Console.ReadLine()) != null)
            {
                if (line == "" && Console.KeyAvailable && 
                    (Console.ReadKey().Key == ConsoleKey.Z || Console.ReadKey().Key == ConsoleKey.D))
                {
                    break;
                }
                lines.Add(line);
            }
            
            return string.Join(Environment.NewLine, lines);
        }

        private static string ConvertMarkdownTableToHtml(string markdownText)
        {
            // Use the Markdig library to convert Markdown to HTML
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string html = Markdown.ToHtml(markdownText, pipeline);

            // Find all table elements and add the dark-table class
            html = Regex.Replace(html, "<table>", "<table class=\"dark-table\">");

            // Wrap the result in HTML structure with CSS
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Markdown Table Converted to HTML</title>
    {DarkThemeCSS}
</head>
<body style=""background-color: #121212; color: #e0e0e0; padding: 20px;"">
    {html}
</body>
</html>";
        }
    }
}
