using System.Text.Json;
using System.Text;
using LanguageLearningTools.BAMFQuestionsToJson.Models;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace BAMFJsonToMarkdown.Commands;

public class JsonToMarkdownCommand : ICommand
{
    private readonly FileInfo _input;
    private readonly FileInfo _output;
    private readonly MarkdownPipeline _pipeline;

    public JsonToMarkdownCommand(FileInfo input, FileInfo output)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _output = output ?? throw new ArgumentNullException(nameof(output));

        // Configure Markdig pipeline with needed extensions
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .UseGridTables()
            .Build();
    }

    public async Task<int> ExecuteAsync()
    {
        if (!_input.Exists)
        {
            Console.Error.WriteLine($"Input file {_input.FullName} does not exist.");
            return 1;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_input.FullName);
            var questions = JsonSerializer.Deserialize<List<BamfQuestion>>(json);

            if (questions == null)
            {
                Console.Error.WriteLine("Failed to deserialize questions.");
                return 1;
            }

            // Sort questions by question number
            var sortedQuestions = new List<BamfQuestion>(questions);
            sortedQuestions.Sort((a, b) => a.QuestionNumber.CompareTo(b.QuestionNumber));

            // Generate markdown using our table builder helper
            var markdown = GenerateMarkdownTable(sortedQuestions);

            // Write the markdown to the output file
            await File.WriteAllTextAsync(_output.FullName, markdown);

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing file: {ex.Message}");
            return 1;
        }
    }

    private string GenerateMarkdownTable(List<BamfQuestion> questions)
    {
        // For compatibility with the expected test output, we'll use the same manual approach
        // but with better structure and more maintainable code
        var markdownBuilder = new StringBuilder();

        // Add table header
        markdownBuilder.AppendLine("| Num | Question | Choices | Answer | Justification |");
        markdownBuilder.AppendLine("|-----|----------|---------|--------|---------------|");

        // Add rows
        foreach (var question in questions)
        {
            AddTableRow(markdownBuilder, question);
        }

        return markdownBuilder.ToString();
    }

    private void AddTableRow(StringBuilder markdownBuilder, BamfQuestion question)
    {
        // Format question text
        var questionText = FormatQuestionText(question);

        // Format choices
        var choicesText = FormatChoices(question);

        // Build the table row
        markdownBuilder.AppendLine(
            $"| {question.QuestionNumber} | {EscapeTableCell(questionText)} | {EscapeTableCell(choicesText)} | {question.German.Answer} | {EscapeTableCell(question.English.Justification)} |");
    }

    private string FormatQuestionText(BamfQuestion question)
    {
        return $"{question.German.Question}\n\n({question.English.Question})";
    }

    private string FormatChoices(BamfQuestion question)
    {
        var choicesBuilder = new StringBuilder();

        AppendChoice(choicesBuilder, "1", question.German.Choice1, question.English.Choice1);
        AppendChoice(choicesBuilder, "2", question.German.Choice2, question.English.Choice2);
        AppendChoice(choicesBuilder, "3", question.German.Choice3, question.English.Choice3);
        AppendChoice(choicesBuilder, "4", question.German.Choice4, question.English.Choice4);

        return choicesBuilder.ToString().TrimEnd();
    }

    private void AppendChoice(StringBuilder builder, string number, string germanText, string englishText)
    {
        builder.AppendLine($"{number}. {germanText}");
        builder.AppendLine($"   ({englishText})\n");
    }

    private static string EscapeTableCell(string text)
    {
        // Replace pipe characters and newlines to prevent table formatting issues
        return text.Replace("|", "\\|")
                  .Replace("\n", "<br>");
    }
}