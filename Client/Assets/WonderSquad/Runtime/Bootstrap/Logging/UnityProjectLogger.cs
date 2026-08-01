using System;
using UnityEngine;
using WonderSquad.Core.Configuration;
using WonderSquad.Core.Logging;

namespace WonderSquad.Bootstrap.Logging
{
    public sealed class UnityProjectLogger : IProjectLogger
    {
        public void Write(
            ProjectLogLevel level,
            string message,
            Exception exception = null)
        {
            var formattedMessage = $"[{ProjectConstants.LogTag}] {message}";

            switch (level)
            {
                case ProjectLogLevel.Debug:
                case ProjectLogLevel.Information:
                    UnityEngine.Debug.Log(formattedMessage);
                    break;
                case ProjectLogLevel.Warning:
                    UnityEngine.Debug.LogWarning(formattedMessage);
                    break;
                case ProjectLogLevel.Error:
                    UnityEngine.Debug.LogError(formattedMessage);
                    if (exception != null)
                    {
                        UnityEngine.Debug.LogException(exception);
                    }

                    break;
                default:
                    UnityEngine.Debug.Log(formattedMessage);
                    break;
            }
        }
    }
}

