using System.Collections.Generic;
using NUnit.Framework;
using WonderSquad.Content.Definitions;
using WonderSquad.Content.Validation;

namespace WonderSquad.Tests.ContentValidation
{
    public sealed class ContentValidatorTests
    {
        [Test]
        public void Validate_AcceptsUniqueStableIds()
        {
            var descriptors = new IContentDescriptor[]
            {
                new Descriptor("level.test", 1),
                new Descriptor("project.configuration", 1)
            };

            var issues = ContentValidator.Validate(descriptors);

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void Validate_ReportsEmptyAndDuplicateIds()
        {
            var descriptors = new IContentDescriptor[]
            {
                new Descriptor(string.Empty, 1),
                new Descriptor("level.test", 1),
                new Descriptor("level.test", 1)
            };

            var issues = ContentValidator.Validate(descriptors);
            var codes = new List<string>();
            foreach (var issue in issues)
            {
                codes.Add(issue.Code);
            }

            Assert.That(codes, Does.Contain("CONTENT_ID_EMPTY"));
            Assert.That(codes, Does.Contain("CONTENT_ID_DUPLICATE"));
        }

        [Test]
        public void Validate_ReportsMissingReferences()
        {
            var descriptors = new IContentDescriptor[]
            {
                new Descriptor(
                    "level.test",
                    1,
                    new[] { "configuration.missing" })
            };

            var issues = ContentValidator.Validate(descriptors);
            var codes = new List<string>();
            foreach (var issue in issues)
            {
                codes.Add(issue.Code);
            }

            Assert.That(codes, Does.Contain("CONTENT_REFERENCE_MISSING"));
        }

        [TestCase("level.sleeping_forest", true)]
        [TestCase("ability.explorer.anchor_route", true)]
        [TestCase("Level.SleepingForest", false)]
        [TestCase("level..test", false)]
        [TestCase(".level", false)]
        public void IsStableIdFormatValid_UsesLowerCaseDotSeparatedTokens(
            string value,
            bool expected)
        {
            Assert.That(ContentValidator.IsStableIdFormatValid(value), Is.EqualTo(expected));
        }

        private sealed class Descriptor : IContentDescriptor
        {
            public Descriptor(
                string stableId,
                int schemaVersion,
                IReadOnlyList<string> referencedIds = null)
            {
                StableId = stableId;
                SchemaVersion = schemaVersion;
                ReferencedIds = referencedIds ?? new string[0];
            }

            public string StableId { get; }

            public int SchemaVersion { get; }

            public IReadOnlyList<string> ReferencedIds { get; }
        }
    }
}
