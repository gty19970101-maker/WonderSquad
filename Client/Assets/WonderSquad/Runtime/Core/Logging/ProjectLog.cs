using System;

namespace WonderSquad.Core.Logging
{
    public static class ProjectLog
    {
        private static IProjectLogger logger = NullProjectLogger.Instance;

        public static void Configure(IProjectLogger newLogger)
        {
            logger = newLogger ?? NullProjectLogger.Instance;
        }

        public static void Debug(string message)
        {
            logger.Write(ProjectLogLevel.Debug, message);
        }

        public static void Information(string message)
        {
            logger.Write(ProjectLogLevel.Information, message);
        }

        public static void Warning(string message)
        {
            logger.Write(ProjectLogLevel.Warning, message);
        }

        public static void Error(string message, Exception exception = null)
        {
            logger.Write(ProjectLogLevel.Error, message, exception);
        }

        private sealed class NullProjectLogger : IProjectLogger
        {
            public static readonly NullProjectLogger Instance = new NullProjectLogger();

            private NullProjectLogger()
            {
            }

            public void Write(
                ProjectLogLevel level,
                string message,
                Exception exception = null)
            {
            }
        }
    }
}

