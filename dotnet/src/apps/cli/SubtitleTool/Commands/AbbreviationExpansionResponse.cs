using System.Text.Json.Serialization;

namespace SubtitleTool.Commands;

public class AbbreviationExpansionResponse
{
    [JsonPropertyName("request")]
    public string? Request { get; set; }

    [JsonPropertyName("abbreviation")]
    public string? Abbreviation { get; set; }

    [JsonPropertyName("expansion")]
    public string? Expansion { get; set; }
}
