using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LanguageLearningTools.Domain
{
    /// <summary>
    /// Defines a contract for subtitle file parsers.
    /// </summary>
    public interface ISubtitleParser
    {
        /// <summary>
        /// Parses a subtitle file stream into a list of subtitle lines.
        /// </summary>
        /// <param name="stream">The subtitle file stream.</param>
        /// <returns>A list of subtitle lines.</returns>
        Task<IReadOnlyList<SubtitleLine>> ParseAsync(Stream stream);
    }
}
