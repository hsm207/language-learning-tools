using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageLearningTools.Domain;

namespace LanguageLearningTools.Application
{
    /// <summary>
    /// Application service responsible for orchestrating the translation of subtitle documents.
    /// This service coordinates between the domain models and translation infrastructure.
    /// </summary>
    public class SubtitleTranslationApplicationService
    {
        private readonly ISubtitleTranslationService _translationService;
        private readonly ISubtitleBatchingStrategy _batchingStrategy;
        private readonly int _batchSize;
        private readonly int _contextSize;
        private readonly int _contextOverlap;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleTranslationApplicationService"/> class.
        /// </summary>
        /// <param name="translationService">The translation service to use for translating subtitle batches.</param>
        /// <param name="batchingStrategy">The batching strategy to use for organizing subtitle lines.</param>
        /// <param name="batchSize">The number of lines to translate in each batch (default: 5).</param>
        /// <param name="contextSize">The number of context lines to provide before each batch (default: 3).</param>
        /// <param name="contextOverlap">The number of lines from the current batch to use as context for the next batch (default: 2).</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public SubtitleTranslationApplicationService(
            ISubtitleTranslationService translationService,
            ISubtitleBatchingStrategy batchingStrategy,
            int batchSize = 5,
            int contextSize = 3,
            int contextOverlap = 2)
        {
            _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
            _batchingStrategy = batchingStrategy ?? throw new ArgumentNullException(nameof(batchingStrategy));
            _batchSize = batchSize;
            _contextSize = contextSize;
            _contextOverlap = contextOverlap;
        }

        /// <summary>
        /// Translates a subtitle document from the source language to the target language.
        /// </summary>
        /// <param name="document">The subtitle document to translate.</param>
        /// <param name="sourceLanguage">The source language of the subtitle content.</param>
        /// <param name="targetLanguage">The target language for translation.</param>
        /// <returns>A new <see cref="SubtitleDocument"/> containing the translated subtitle lines.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="document"/> is null.</exception>
        /// <remarks>
        /// This method uses the configured batching strategy to organize subtitle lines into batches,
        /// then translates each batch using the translation service. The results are combined into
        /// a new subtitle document with both original and translated text.
        /// </remarks>
        public async Task<SubtitleDocument> TranslateDocumentAsync(
            SubtitleDocument document,
            Lang sourceLanguage,
            Lang targetLanguage)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            // Handle empty documents gracefully
            if (!document.Lines.Any())
            {
                return new SubtitleDocument(Array.Empty<SubtitleLine>());
            }

            // Create batches using our strategy - let it decide how to group the lines
            var batches = _batchingStrategy.CreateBatches(document.Lines.ToList(), _batchSize, _contextSize, _contextOverlap);
            
            // Translate each batch and collect the results
            var translatedLines = new List<SubtitleLine>();
            
            foreach (var batch in batches)
            {
                // Create the batch request with the lines from the batch
                var batchRequest = new SubtitleBatchRequest
                {
                    ContextLines = batch.Context.ToList(),
                    LinesToTranslate = batch.Lines.ToList()
                };
                
                // Get the translation for this batch
                var batchResponse = await _translationService.TranslateBatchAsync(batchRequest, sourceLanguage, targetLanguage);
                
                // Add the translated lines to our collection
                translatedLines.AddRange(batchResponse.TranslatedLines);
            }

            // Create a new document with all the translated lines
            return new SubtitleDocument(translatedLines);
        }
    }
}
