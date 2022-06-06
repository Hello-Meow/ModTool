using System;
using UnityEngine;

namespace ModTool.Shared
{
    /// <summary>
    /// Filter level for logging messages to the console or log file.
    /// </summary>
    public enum LogLevel { Error = 1, Warning = 2, Info = 3, Debug = 4 }

    /// <summary>
    /// A class for logging filtered messages.
    /// </summary>
    public class LogUtility
    {
        /// <summary>
        /// Log a debug message.
        /// </summary>
        /// <param name="message">The debug message.</param>
        public static void LogDebug(object message)
        {
            if (ModToolSettings.logLevel >= LogLevel.Debug)
                Debug.Log(message);
        }

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void LogInfo(object message)
        {
            if (ModToolSettings.logLevel >= LogLevel.Info)
                Debug.Log(message);
        }

        /// <summary>
        /// Log a warning.
        /// </summary>
        /// <param name="message">The warning message.</param>
        public static void LogWarning(object message)
        {
            if (ModToolSettings.logLevel >= LogLevel.Warning)
                Debug.LogWarning(message);
        }

        /// <summary>
        /// Log an error.
        /// </summary>
        /// <param name="message">The error message</param>
        public static void LogError(object message)
        {
            if (ModToolSettings.logLevel >= LogLevel.Error)
                Debug.LogError(message);
        }

        /// <summary>
        /// Log an exception.
        /// </summary>
        /// <param name="exception">The exception</param>
        public static void LogException(Exception exception)
        {
            if (ModToolSettings.logLevel >= LogLevel.Error)
                Debug.LogException(exception);
        }
    }
}
