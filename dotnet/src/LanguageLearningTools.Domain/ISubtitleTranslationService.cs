using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningTools.Domain;

namespace LanguageLearningTools.Domain
{
    /// <summary>
    /// Translates text from a source language to a target language asynchronously.
    /// </summary>
    public interface ISubtitleTranslationService
    {
        /// <summary>
        /// Translates a batch of subtitle lines from the source language to the target language, with full context and metadata.
        /// </summary>
        /// <param name="request">The batch request containing context and lines to translate.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <returns>The batch response containing translated lines with metadata.</returns>
        Task<SubtitleBatchResponse> TranslateBatchAsync(SubtitleBatchRequest request, Lang sourceLanguage, Lang targetLanguage);
    }
}
