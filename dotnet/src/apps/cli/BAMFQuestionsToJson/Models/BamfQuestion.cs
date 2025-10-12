#pragma warning disable CA1515 // Types should be internal
using System.Text.Json.Serialization;

namespace LanguageLearningTools.BAMFQuestionsToJson.Models;

public sealed class BamfQuestion
{
    [JsonPropertyName("num")]
    public int QuestionNumber { get; set; } = new();

    [JsonPropertyName("de")]
    public GermanContent German { get; set; } = new();

    [JsonPropertyName("en")]
    public EnglishContent English { get; set; } = new();
}

public sealed class GermanContent
{
    [JsonPropertyName("Question")]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("Choice1")]
    public string Choice1 { get; set; } = string.Empty;

    [JsonPropertyName("Choice2")]
    public string Choice2 { get; set; } = string.Empty;

    [JsonPropertyName("Choice3")]
    public string Choice3 { get; set; } = string.Empty;

    [JsonPropertyName("Choice4")]
    public string Choice4 { get; set; } = string.Empty;

    [JsonPropertyName("Answer")]
    public int Answer { get; set; }
}

public sealed class EnglishContent
{
    [JsonPropertyName("Question")]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("Choice1")]
    public string Choice1 { get; set; } = string.Empty;

    [JsonPropertyName("Choice2")]
    public string Choice2 { get; set; } = string.Empty;

    [JsonPropertyName("Choice3")]
    public string Choice3 { get; set; } = string.Empty;

    [JsonPropertyName("Choice4")]
    public string Choice4 { get; set; } = string.Empty;

    [JsonPropertyName("Justification")]
    public string Justification { get; set; } = string.Empty;
}