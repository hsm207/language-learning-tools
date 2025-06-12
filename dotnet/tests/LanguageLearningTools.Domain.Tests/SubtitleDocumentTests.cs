using System;
using System.Collections.Generic;
using System.Linq;
using LanguageLearningTools.Domain;
using Xunit;

namespace LanguageLearningTools.Domain.Tests
{
    public class SubtitleDocumentTests
    {
        [Fact]
        public void Constructor_Should_Throw_If_Lines_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new SubtitleDocument(null!));
        }

        [Fact]
        public void Constructor_Should_Store_Lines_Correctly()
        {
            var lines = new[]
            {
                new SubtitleLine(TimeSpan.Zero, TimeSpan.FromSeconds(1), "Hallo"),
                new SubtitleLine(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), "Welt")
            };
            var doc = new SubtitleDocument(lines);
            Assert.Equal(2, doc.Lines.Count);
            Assert.Equal("Hallo", doc.Lines[0].Text);
        }

        [Fact]
        public void Constructor_Should_Store_Lines_As_ReadOnly()
        {
            var lines = new[]
            {
                new SubtitleLine(TimeSpan.Zero, TimeSpan.FromSeconds(1), "Hallo"),
                new SubtitleLine(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), "Welt")
            };
            var doc = new SubtitleDocument(lines);

            // Attempt to cast to ICollection and modify, should throw
            var collection = Assert.IsAssignableFrom<ICollection<SubtitleLine>>(doc.Lines);
            Assert.True(collection.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => collection.Add(new SubtitleLine(TimeSpan.Zero, TimeSpan.Zero, "Test")));
            Assert.Throws<NotSupportedException>(() => collection.Remove(doc.Lines[0]));
        }

    }
}
