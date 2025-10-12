namespace LanguageLearningTools.BAMFFragenkatalogScraper.Tests;

using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

public class ProgramTests
{
    private readonly BAMFScraperConfiguration _defaultConfig;

    public ProgramTests()
    {
        _defaultConfig = new BAMFScraperConfiguration();
    }

    [Fact]
    public void DefaultConfig_ShouldHaveCorrectValues()
    {
        Assert.Equal("https://oet.bamf.de/ords/oetut/f?p=514:1::::::", _defaultConfig.TestUrl);
        Assert.Equal("Berlin", _defaultConfig.BundesLand);
        Assert.Equal("screenshots", _defaultConfig.ScreenshotDirectory);
        Assert.Equal("#P1_BUL_ID", _defaultConfig.StateDropdownSelector);
        Assert.Equal("#R59645205843215396", _defaultConfig.QuestionElementSelector);
        Assert.Equal("nächste Aufgabe >", _defaultConfig.NextQuestionButtonText);
        Assert.Equal("Zum Fragenkatalog", _defaultConfig.StartButtonText);
        Assert.Equal(310, _defaultConfig.TotalQuestions);
    }

    [Fact]
    public async Task UIElements_ShouldExistAndBeInteractable()
    {
        // Setup Playwright
        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions 
        { 
            Headless = true
        });
        var page = await browser.NewPageAsync();

        try
        {
            // Navigate to the test page
            await page.GotoAsync(_defaultConfig.TestUrl);

            // Check if state dropdown exists and is visible
            var stateDropdown = page.Locator(_defaultConfig.StateDropdownSelector);
            await Expect(stateDropdown).ToBeVisibleAsync();

            // Verify Berlin is available in the dropdown
            var options = await page.Locator($"{_defaultConfig.StateDropdownSelector} option").AllTextContentsAsync();
            Assert.Contains(_defaultConfig.BundesLand, options);

            // Select Berlin and verify it's selected
            await page.SelectOptionAsync(_defaultConfig.StateDropdownSelector, _defaultConfig.BundesLand);
            await Expect(page.Locator($"{_defaultConfig.StateDropdownSelector} option:checked")).ToHaveTextAsync(_defaultConfig.BundesLand);

            // Verify start button exists and is clickable
            var startButton = page.GetByText(_defaultConfig.StartButtonText);
            await Expect(startButton).ToBeVisibleAsync();
            await Expect(startButton).ToBeEnabledAsync();
            await startButton.ClickAsync();

            // After clicking start, verify question element exists
            var questionElement = page.Locator(_defaultConfig.QuestionElementSelector);
            await Expect(questionElement).ToBeVisibleAsync();

            // Verify next question button exists
            var nextButton = page.GetByText(_defaultConfig.NextQuestionButtonText);
            await Expect(nextButton).ToBeVisibleAsync();
            await Expect(nextButton).ToBeEnabledAsync();
        }
        finally
        {
            await browser.CloseAsync();
        }
    }
}
