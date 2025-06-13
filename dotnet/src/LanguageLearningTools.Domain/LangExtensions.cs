using System;

namespace LanguageLearningTools.Domain
{
    /// <summary>
    /// Extension methods for the Lang enum to map to language codes.
    /// </summary>
    public static class LangExtensions
    {
        /// <summary>
        /// Gets the language code (e.g., "de", "en") for the given Lang value.
        /// </summary>
        /// <param name="lang">The language enum value.</param>
        /// <returns>The language code as required by translation APIs.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the Lang value is not supported.</exception>
        public static string ToCode(this Lang lang)
        {
            return lang switch
            {
                Lang.German => "de",
                Lang.English => "en",
                _ => throw new ArgumentOutOfRangeException(nameof(lang), lang, null)
            };
        }

        /// <summary>
        /// Gets the display name for the given Lang value.
        /// </summary>
        /// <param name="lang">The language enum value.</param>
        /// <returns>The display name of the language.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the Lang value is not supported.</exception>
        public static string GetDisplayName(this Lang lang)
        {
            return lang switch
            {
                Lang.German => "German",
                Lang.English => "English",
                _ => throw new ArgumentOutOfRangeException(nameof(lang), lang, null)
            };
        }

        /// <summary>
        /// Tries to parse a language code (e.g., "de", "en") to a Lang enum value.
        /// </summary>
        /// <param name="code">The language code to parse.</param>
        /// <param name="lang">When this method returns, contains the Lang value if the parse was successful, or default if unsuccessful.</param>
        /// <returns>true if the parse was successful; otherwise, false.</returns>
        public static bool TryParseFromCode(string? code, out Lang lang)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                lang = default;
                return false;
            }

            var normalizedCode = code.ToLowerInvariant().Trim();
            
            lang = normalizedCode switch
            {
                "de" or "german" => Lang.German,
                "en" or "english" => Lang.English,
                _ => default
            };

            // Check if we successfully matched a valid language code
            return normalizedCode is "de" or "german" or "en" or "english";
        }
    }
}
