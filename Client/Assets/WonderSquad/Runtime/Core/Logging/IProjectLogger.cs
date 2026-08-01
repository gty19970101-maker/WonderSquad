using System;

namespace WonderSquad.Core.Logging
{
    public interface IProjectLogger
    {
        void Write(ProjectLogLevel level, string message, Exception exception = null);
    }
}

