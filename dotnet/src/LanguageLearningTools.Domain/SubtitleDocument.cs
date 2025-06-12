using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace LanguageLearningTools.Domain
{
    /// <summary>
    /// Represents a collection of subtitle lines as a value object.
    /// </summary>
    public sealed class SubtitleDocument
    {
        /// <summary>
        /// Gets the subtitle lines in this document.
        /// </summary>
        public IReadOnlyList<SubtitleLine> Lines { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleDocument"/> class.
        /// </summary>
        /// <param name="lines">The subtitle lines to include in the document.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="lines"/> is null.</exception>
        public SubtitleDocument(IEnumerable<SubtitleLine> lines)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));
            Lines = new ReadOnlyCollection<SubtitleLine>(lines.ToList());
        }

    }
}
