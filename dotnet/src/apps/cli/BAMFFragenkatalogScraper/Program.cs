[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("BAMFFragenkatalogScraper.Tests")]

namespace LanguageLearningTools.BAMFFragenkatalogScraper;

using Microsoft.Playwright;
using System.CommandLine;
using static Microsoft.Playwright.Assertions;

internal class BAMFScraperConfiguration
{
    public string TestUrl { get; set; } = "https://oet.bamf.de/ords/oetut/f?p=514:1::::::";
    public string BundesLand { get; set; } = "Berlin";
    public int QuestionDelayMs { get; set; } = 5000;
    public string ScreenshotDirectory { get; set; } = "screenshots";
    public string StateDropdownSelector { get; set; } = "#P1_BUL_ID";
    public string QuestionElementSelector { get; set; } = "#R57608719341524241";
    public string NextQuestionButtonText { get; set; } = "nächste Aufgabe >";
    public string StartButtonText { get; set; } = "Zum Fragenkatalog";
    public int TotalQuestions { get; set; } = 310;
}

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var urlOption = new Option<string>(
            name: "--url",
            description: "Test URL",
            getDefaultValue: () => "https://oet.bamf.de/ords/oetut/f?p=514:1::::::"
        );

        var bundeslandOption = new Option<string>(
            name: "--bundesland",
            description: "Federal state",
            getDefaultValue: () => "Berlin"
        );

        var outputOption = new Option<string>(
            name: "--output",
            description: "Screenshot output directory",
            getDefaultValue: () => "screenshots"
        );

        var rootCommand = new RootCommand("BAMF Interaktiver Fragenkatalog Scraper");
        rootCommand.AddOption(urlOption);
        rootCommand.AddOption(bundeslandOption);
        rootCommand.AddOption(outputOption);

        rootCommand.SetHandler(async (url, bundesland, output) =>
        {
            var config = new BAMFScraperConfiguration
            {
                TestUrl = url,
                BundesLand = bundesland,
                ScreenshotDirectory = output
            };

            Console.WriteLine($"Starting to scrape with following configuration:");
            Console.WriteLine($"URL: {config.TestUrl}");
            Console.WriteLine($"Bundesland: {config.BundesLand}");
            Console.WriteLine($"Output directory: {config.ScreenshotDirectory}");
            Console.WriteLine();

            SetDefaultExpectTimeout(5_000);
            using var playwright = await Playwright.CreateAsync();
            var options = new BrowserTypeLaunchOptions
            {
                Headless = false,
                SlowMo = 50
            };

            // Ensure screenshot directory exists
            Directory.CreateDirectory(config.ScreenshotDirectory);

            await using var browser = await playwright.Chromium.LaunchAsync(options);
            var page = await browser.NewPageAsync();
            await page.GotoAsync(config.TestUrl);

            // Select state from the dropdown
            await page.SelectOptionAsync(config.StateDropdownSelector, config.BundesLand);

            // Assert that state is selected using Playwright assertions
            await Expect(page.Locator($"{config.StateDropdownSelector} option:checked")).ToHaveTextAsync(config.BundesLand);

            // Click start button
            await page.GetByText(config.StartButtonText).ClickAsync();

            bool isLastQuestion = false;
            while (!isLastQuestion)
            {
                // Get the current question number
                var headerText = await page.Locator("td.RegionHeader").Filter(new() { HasText = "Aufgabe" }).TextContentAsync();
                
                // Get the question number from the header text
                var questionNumber = headerText?.Split(' ')[1].PadLeft(3, '0');

                // Screenshot just the question element
                var questionElement = page.Locator(config.QuestionElementSelector);
                await questionElement.ScreenshotAsync(new()
                {
                    Path = Path.Combine(config.ScreenshotDirectory, $"question_{questionNumber}.png")
                });

                // Check if we've reached the last question
                isLastQuestion = headerText == $"Aufgabe {config.TotalQuestions} von {config.TotalQuestions}";

                if (!isLastQuestion)
                {
                    // Move to next question
                    await page.GetByText(config.NextQuestionButtonText).ClickAsync();
                    // Add a delay between questions to be polite
                    await Task.Delay(config.QuestionDelayMs);
                    // Wait for network to be idle
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                }
            }

            Console.WriteLine("Scrape completed - all questions processed");
        }, urlOption, bundeslandOption, outputOption);

        return await rootCommand.InvokeAsync(args);
    }
}
