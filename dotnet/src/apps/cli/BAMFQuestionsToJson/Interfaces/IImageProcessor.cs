using LanguageLearningTools.BAMFQuestionsToJson.Models;

namespace LanguageLearningTools.BAMFQuestionsToJson.Interfaces;

/// <summary>
/// Defines operations for processing image files containing BAMF test questions.
/// </summary>
internal interface IImageProcessor
{
    /// <summary>
    /// Processes an image file containing a BAMF test question.
    /// </summary>
    /// <param name="imageFile">Path to the image file to process.</param>
    /// <returns>A BamfQuestion object containing the extracted question data, or null if processing fails.</returns>
    Task<BamfQuestion?> ProcessImage(string imageFile);
}