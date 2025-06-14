using System;
using System.Net;
using System.Reflection;
using LanguageLearningTools.Infrastructure;
using Microsoft.SemanticKernel;
using Xunit;

namespace LanguageLearningTools.Infrastructure.Tests
{
    /// <summary>
    /// Unit tests for GeminiSubtitleTranslationService, focusing on internal logic and retry behavior.
    /// </summary>
    public class GeminiSubtitleTranslationServiceTests
    {
        /// <summary>
        /// Test that API_KEY_INVALID errors should not be retried
        /// </summary>
        [Fact]
        public void ShouldRetryException_WithApiKeyInvalidError_ReturnsFalse()
        {
            // Arrange
            var responseContent = @"{
                ""error"": {
                    ""code"": 400,
                    ""message"": ""API key not valid. Please pass a valid API key."",
                    ""status"": ""INVALID_ARGUMENT"",
                    ""details"": [
                        {
                            ""@type"": ""type.googleapis.com/google.rpc.ErrorInfo"",
                            ""reason"": ""API_KEY_INVALID"",
                            ""domain"": ""googleapis.com""
                        }
                    ]
                }
            }";

            var httpException = new HttpOperationException(HttpStatusCode.BadRequest, "API key error", responseContent, null);

            // Act
            var shouldRetry = InvokeShouldRetryException(httpException);

            // Assert
            Assert.False(shouldRetry, "API_KEY_INVALID errors should not be retried");
        }

        /// <summary>
        /// Test that all other HttpOperationException errors should be retried
        /// </summary>
        [Fact]
        public void ShouldRetryException_WithOtherHttpErrors_ReturnsTrue()
        {
            // Arrange - Test a different error type that should be retried
            var responseContent = @"{
                ""error"": {
                    ""code"": 429,
                    ""message"": ""Quota exceeded"",
                    ""status"": ""RESOURCE_EXHAUSTED""
                }
            }";

            var httpException = new HttpOperationException(HttpStatusCode.TooManyRequests, "Rate limited", responseContent, null);

            // Act
            var shouldRetry = InvokeShouldRetryException(httpException);

            // Assert
            Assert.True(shouldRetry, "Non-API_KEY_INVALID errors should be retried");
        }

        /// <summary>
        /// Helper method to invoke the private ShouldRetryException method using reflection
        /// </summary>
        private static bool InvokeShouldRetryException(HttpOperationException exception)
        {
            var method = typeof(GeminiSubtitleTranslationService).GetMethod("ShouldRetryException", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            if (method == null)
                throw new InvalidOperationException("ShouldRetryException method not found");

            return (bool)method.Invoke(null, new object[] { exception });
        }
    }
}
