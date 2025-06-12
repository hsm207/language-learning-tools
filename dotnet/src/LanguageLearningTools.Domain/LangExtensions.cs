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
    }
}
