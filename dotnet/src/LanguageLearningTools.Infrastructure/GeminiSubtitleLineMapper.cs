using System;
using System.Net;
using LanguageLearningTools.Domain;

namespace LanguageLearningTools.Infrastructure
{
    /// <summary>
    /// Provides bidirectional mapping between domain SubtitleLine and Gemini API representations.
    /// Handles the translation boundary between our clean domain model and external API requirements.
    /// </summary>
    /// <remarks>
    /// <para><strong>Translation Flow:</strong></para>
    /// <list type="number">
    /// <item><description><strong>Outbound:</strong> Domain → Gemini (via ToGeminiDto) for API requests</description></item>
    /// <item><description><strong>Inbound:</strong> Gemini → Domain (via FromGeminiDto) for processing responses</description></item>
    /// </list>
    /// <para><strong>Key Transformations:</strong></para>
    /// <list type="bullet">
    /// <item><description>TimeSpan ↔ String timestamp conversion ("hh:mm:ss.fff" format)</description></item>
    /// <item><description>HTML entity decoding for proper Unicode character display (emojis, special chars)</description></item>
    /// <item><description>Null safety and validation</description></item>
    /// </list>
    /// </remarks>
    public static class GeminiSubtitleLineMapper
    {
        /// <summary>
        /// Converts a domain subtitle line to Gemini API format for outbound requests.
        /// </summary>
        /// <param name="line">The domain subtitle line to convert</param>
        /// <returns>A Gemini-compatible representation with string timestamps</returns>
        /// <exception cref="ArgumentNullException">Thrown when line is null</exception>
        public static GeminiSubtitleLine ToGeminiDto(SubtitleLine line)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));
            return new GeminiSubtitleLine(
                line.Start.ToString(@"hh\:mm\:ss\.fff"),
                line.End.ToString(@"hh\:mm\:ss\.fff"),
                line.Text,
                line.TranslatedText
            );
        }

        /// <summary>
        /// Converts a Gemini API response back to our domain model for internal processing.
        /// </summary>
        /// <param name="dto">The Gemini API response data to convert</param>
        /// <returns>A domain subtitle line with proper TimeSpan timestamps and decoded text</returns>
        /// <exception cref="ArgumentNullException">Thrown when dto is null</exception>
        /// <remarks>
        /// <strong>HTML Entity Decoding:</strong> Gemini sometimes returns HTML entities (like &amp;ouml; or &amp;#129412;) 
        /// instead of proper Unicode characters. This method automatically decodes them to ensure 
        /// emojis and special characters display correctly in the final output.
        /// </remarks>
        public static SubtitleLine FromGeminiDto(GeminiSubtitleLine dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            
            // Decode HTML entities in the translated text to preserve Unicode characters (emojis, etc.)
            var decodedTranslatedText = WebUtility.HtmlDecode(dto.TranslatedText);
            
            return new SubtitleLine(
                TimeSpan.ParseExact(dto.Start, @"hh\:mm\:ss\.fff", null),
                TimeSpan.ParseExact(dto.End, @"hh\:mm\:ss\.fff", null),
                dto.Text,
                decodedTranslatedText
            );
        }
    }
}
