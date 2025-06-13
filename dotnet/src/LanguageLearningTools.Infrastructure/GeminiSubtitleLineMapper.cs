using System;
using LanguageLearningTools.Domain;

namespace LanguageLearningTools.Infrastructure
{
    /// <summary>
    /// Provides mapping between domain SubtitleLine and GeminiSubtitleLineDto.
    /// </summary>
    /// <remarks>
    /// Ensures consistent conversion between domain and Gemini DTOs, including timestamp formatting.
    /// </remarks>
    public static class GeminiSubtitleLineMapper
    {
        /// <summary>
        /// Maps a domain <see cref="SubtitleLine"/> to a <see cref="GeminiSubtitleLineDto"/>.
        /// </summary>
        /// <param name="line">The domain subtitle line.</param>
        /// <returns>The Gemini DTO.</returns>
        public static GeminiSubtitleLineDto ToGeminiDto(SubtitleLine line)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));
            return new GeminiSubtitleLineDto(
                line.Start.ToString(@"hh\:mm\:ss\.fff"),
                line.End.ToString(@"hh\:mm\:ss\.fff"),
                line.Text,
                line.TranslatedText
            );
        }

        /// <summary>
        /// Maps a <see cref="GeminiSubtitleLineDto"/> to a domain <see cref="SubtitleLine"/>.
        /// </summary>
        /// <param name="dto">The Gemini DTO.</param>
        /// <returns>The domain subtitle line.</returns>
        public static SubtitleLine FromGeminiDto(GeminiSubtitleLineDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            return new SubtitleLine(
                TimeSpan.ParseExact(dto.Start, @"hh\:mm\:ss\.fff", null),
                TimeSpan.ParseExact(dto.End, @"hh\:mm\:ss\.fff", null),
                dto.Text,
                dto.TranslatedText
            );
        }
    }
}
