using System;
using NUnit.Framework;
using WonderSquad.Core.Logging;

namespace WonderSquad.Tests.EditMode
{
    public sealed class ProjectLogTests
    {
        [TearDown]
        public void TearDown()
        {
            ProjectLog.Configure(null);
        }

        [Test]
        public void Configure_RoutesMessagesToTheSelectedLogger()
        {
            var logger = new RecordingLogger();
            ProjectLog.Configure(logger);

            ProjectLog.Information("ready");

            Assert.That(logger.LastLevel, Is.EqualTo(ProjectLogLevel.Information));
            Assert.That(logger.LastMessage, Is.EqualTo("ready"));
        }

        [Test]
        public void Configure_WithNull_UsesSafeNoOpLogger()
        {
            ProjectLog.Configure(null);

            Assert.DoesNotThrow(() => ProjectLog.Warning("ignored safely"));
        }

        private sealed class RecordingLogger : IProjectLogger
        {
            public ProjectLogLevel LastLevel { get; private set; }

            public string LastMessage { get; private set; }

            public void Write(
                ProjectLogLevel level,
                string message,
                Exception exception = null)
            {
                LastLevel = level;
                LastMessage = message;
            }
        }
    }
}

