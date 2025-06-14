using System;
using System.Collections.Generic;
using LanguageLearningTools.Domain;
using LanguageLearningTools.Infrastructure;
using Xunit;

namespace LanguageLearningTools.Infrastructure.Tests
{
    public class SubtitleJsonSerializerTests
    {
        [Fact]
        public void ToJson_SerializesCorrectly()
        {
            var lines = new List<SubtitleLine>
            {
                new SubtitleLine(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), "Hello!"),
                new SubtitleLine(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(4), "World!")
            };

            var json = SubtitleJsonSerializer.ToJson(lines);

            Assert.Contains("\"start\": \"00:00:01.000\"", json);
            Assert.Contains("\"end\": \"00:00:02.000\"", json);
            Assert.Contains("\"text\": \"Hello!\"", json);
            Assert.Contains("\"start\": \"00:00:03.000\"", json);
            Assert.Contains("\"end\": \"00:00:04.000\"", json);
            Assert.Contains("\"text\": \"World!\"", json);
        }
    }
}
