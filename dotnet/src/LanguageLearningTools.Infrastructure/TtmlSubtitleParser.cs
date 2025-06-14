using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using LanguageLearningTools.Domain;

namespace LanguageLearningTools.Infrastructure
{
    /// <summary>
    /// Parses TTML subtitle files into structured subtitle lines.
    /// </summary>
    public class TtmlSubtitleParser : ISubtitleParser
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<SubtitleLine>> ParseAsync(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            var doc = await XDocument.LoadAsync(stream, LoadOptions.None, default);
            XNamespace ns = "http://www.w3.org/ns/ttml";
            var lines = new List<SubtitleLine>();

            foreach (var p in doc.Descendants(ns + "p"))
            {
                var begin = p.Attribute("begin")?.Value;
                var end = p.Attribute("end")?.Value;
                var text = p.Value?.Trim();

                if (TimeSpan.TryParse(begin, out var start) &&
                    TimeSpan.TryParse(end, out var finish) &&
                    !string.IsNullOrWhiteSpace(text))
                {
                    lines.Add(new SubtitleLine(start, finish, text));
                }
            }

            return lines;
        }
    }
}
