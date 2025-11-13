using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FileSharePortal.Helpers
{
    /// <summary>
    /// Helper class for logging throughout the application
    /// </summary>
    public static class LoggingHelper
    {
        private static readonly Dictionary<Type, ILog> Loggers = new Dictionary<Type, ILog>();
        private static readonly object LockObject = new object();

        /// <summary>
        /// Gets logger for the specified type
        /// </summary>
        public static ILog GetLogger(Type type)
        {
            lock (LockObject)
            {
                if (!Loggers.ContainsKey(type))
                {
                    Loggers[type] = LogManager.GetLogger(type);
                }
                return Loggers[type];
            }
        }

        /// <summary>
        /// Gets logger for the calling class
        /// </summary>
        public static ILog GetLogger()
        {
            var frame = new System.Diagnostics.StackFrame(1, false);
            var type = frame.GetMethod().DeclaringType;
            return GetLogger(type);
        }

        /// <summary>
        /// Logs method entry with parameters
        /// </summary>
        public static void LogWithParams(ILog logger, string methodName, params object[] parameters)
        {
            if (logger.IsDebugEnabled)
            {
                var paramInfo = parameters != null && parameters.Length > 0
                    ? $" with parameters: {FormatParameters(parameters)}"
                    : " with no parameters";

                logger.Debug($"Entering {methodName}{paramInfo}");
            }
        }

        /// <summary>
        /// Logs an error
        /// </summary>
        public static void LogError(ILog logger, string message, Exception ex = null)
        {
            if (ex != null)
            {
                logger.Error(message, ex);
            }
            else
            {
                logger.Error(message);
            }
        }

        /// <summary>
        /// Formats parameters for logging
        /// </summary>
        private static string FormatParameters(object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return "none";

            var formatted = new List<string>();
            for (int i = 0; i < parameters.Length; i++)
            {
                formatted.Add($"[{i}]={FormatObject(parameters[i])}");
            }

            return string.Join(", ", formatted);
        }

        /// <summary>
        /// Formats an object for logging (handles nulls and sensitive data)
        /// </summary>
        private static string FormatObject(object obj)
        {
            if (obj == null)
                return "null";

            if (obj is string str)
            {
                // Mask passwords and sensitive data
                if (str.Length > 100)
                    return $"{str.Substring(0, 100)}... (truncated, length={str.Length})";
                return $"\"{str}\"";
            }

            if (obj.GetType().IsPrimitive || obj is decimal || obj is DateTime)
                return obj.ToString();

            if (obj is System.Collections.IEnumerable enumerable && !(obj is string))
            {
                var items = enumerable.Cast<object>().Take(5).Select(FormatObject);
                var count = enumerable.Cast<object>().Count();
                return $"[{string.Join(", ", items)}{(count > 5 ? "..." : "")}] (count={count})";
            }

            // For complex objects, just return type name
            return $"<{obj.GetType().Name}>";
        }

        /// <summary>
        /// Masks sensitive parameter names (passwords, tokens, etc.)
        /// </summary>
        public static Dictionary<string, object> MaskSensitiveData(Dictionary<string, object> parameters)
        {
            var masked = new Dictionary<string, object>();
            var sensitiveKeys = new[] { "password", "token", "secret", "key", "apikey", "api_key" };

            foreach (var kvp in parameters)
            {
                if (sensitiveKeys.Any(sk => kvp.Key.ToLower().Contains(sk)))
                {
                    masked[kvp.Key] = "***MASKED***";
                }
                else
                {
                    masked[kvp.Key] = kvp.Value;
                }
            }

            return masked;
        }
    }
}
