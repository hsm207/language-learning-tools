using System.Collections.Generic;
using System.Text.Json;
using LanguageLearningTools.Domain;

namespace LanguageLearningTools.Infrastructure
{
    /// <summary>
    /// Serializes subtitle lines to JSON.
    /// </summary>
    public static class SubtitleJsonSerializer
    {
        /// <summary>
        /// Serializes subtitle lines to a JSON array with start, end, and text.
        /// </summary>
        /// <param name="lines">Subtitle lines.</param>
        /// <returns>JSON string.</returns>
        public static string ToJson(IEnumerable<SubtitleLine> lines)
        {
            var items = new List<object>();
            foreach (var line in lines)
            {
                items.Add(new
                {
                    start = line.Start.ToString(@"hh\:mm\:ss\.fff"),
                    end = line.End.ToString(@"hh\:mm\:ss\.fff"),
                    text = line.Text
                });
            }
            return JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
