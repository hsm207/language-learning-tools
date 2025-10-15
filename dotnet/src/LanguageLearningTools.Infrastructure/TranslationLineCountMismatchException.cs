using System;

namespace LanguageLearningTools.Infrastructure
{
    /// <summary>
    /// Exception thrown when the number of translated lines from the API does not match the number of input lines.
    /// </summary>
    public class TranslationLineCountMismatchException : Exception
    {
        public int ExpectedCount { get; }
        public int ActualCount { get; }

        public TranslationLineCountMismatchException(int expectedCount, int actualCount)
            : base($"Expected {expectedCount} translations, got {actualCount}. Each input line must produce exactly one translation.")
        {
            ExpectedCount = expectedCount;
            ActualCount = actualCount;
        }
    }
}
