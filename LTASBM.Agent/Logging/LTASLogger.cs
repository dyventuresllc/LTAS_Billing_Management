using LTASBM.Agent.Utilites;
using Relativity.API;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace LTASBM.Agent.Logging
{
    public class LTASLogger : ILTASLogger
    {
        private readonly IDBContext _eddsDbContext;
        private readonly string _applicationName;
        private readonly string _source;
        private readonly LTASBMHelper _ltasHelper;

        public LTASLogger(IDBContext eddsDbContext, IHelper helper, IAPILog logger, string applicationName, string source)
        {
            _eddsDbContext = eddsDbContext ?? throw new ArgumentNullException(nameof(eddsDbContext));
            _applicationName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _ltasHelper = new LTASBMHelper(helper, logger.ForContext<LTASLogger>());
        }

        // Original implementation methods
        public void LogInformation(string message)
        {
            Log("Information", message);
        }

        public void LogWarning(string message)
        {
            Log("Warning", message);
        }

        public void LogError(string message)
        {
            Log("Error", message);
        }

        public void LogError(Exception ex, string message)
        {
            Log("Error", message, ex);
        }

        public void LogDebug(string message)
        {
            Log("Debug", message);
        }

        // New overloads with improved formatting
        public void LogInformation(string messageTemplate, params object[] propertyValues)
        {
            try
            {
                var message = FormatMessage(messageTemplate, propertyValues);
                Log("Information", message);
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error formatting information log message");
                Log("Information", messageTemplate); // Fallback to original message
            }
        }

        public void LogWarning(string messageTemplate, params object[] propertyValues)
        {
            try
            {
                var message = FormatMessage(messageTemplate, propertyValues);
                Log("Warning", message);
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error formatting warning log message");
                Log("Warning", messageTemplate);
            }
        }

        public void LogError(string messageTemplate, params object[] propertyValues)
        {
            try
            {
                var message = FormatMessage(messageTemplate, propertyValues);
                Log("Error", message);
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error formatting error log message");
                Log("Error", messageTemplate);
            }
        }

        public void LogError(Exception ex, string messageTemplate, params object[] propertyValues)
        {
            try
            {
                var message = FormatMessage(messageTemplate, propertyValues);
                Log("Error", message, ex);
            }
            catch (Exception formatEx)
            {
                _ltasHelper.Logger.LogError(formatEx, "Error formatting error log message with exception");
                Log("Error", messageTemplate, ex);
            }
        }

        public void LogDebug(string messageTemplate, params object[] propertyValues)
        {
            try
            {
                var message = FormatMessage(messageTemplate, propertyValues);
                Log("Debug", message);
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error formatting debug log message");
                Log("Debug", messageTemplate);
            }
        }

        private string FormatMessage(string messageTemplate, object[] propertyValues)
        {
            if (string.IsNullOrEmpty(messageTemplate) || propertyValues == null || propertyValues.Length == 0)
            {
                return messageTemplate ?? string.Empty;
            }

            try
            {
                var message = messageTemplate;

                // First try to replace named parameters
                for (int i = 0; i < propertyValues.Length; i++)
                {
                    var value = propertyValues[i]?.ToString() ?? "null";

                    // Replace both numbered {0} and named {ParameterName} placeholders
                    message = message.Replace($"{{{i}}}", value);

                    // If there are any remaining placeholders, attempt to replace them
                    if (message.Contains("{") && message.Contains("}"))
                    {
                        var start = message.IndexOf('{');
                        var end = message.IndexOf('}', start);
                        if (start >= 0 && end > start)
                        {
                            var placeholder = message.Substring(start, end - start + 1);
                            message = message.Replace(placeholder, value);
                        }
                    }
                }

                return message;
            }
            catch
            {
                // If complex formatting fails, fall back to simple concatenation
                return $"{messageTemplate} [{string.Join(", ", propertyValues)}]";
            }
        }

        private void Log(string level, string message, Exception exception = null)
        {
            try
            {
                string sql = @"
                    INSERT INTO QE.ApplicationLog_Billing 
                    (LogDateTime, LogLevel, ApplicationName, Source, Message, 
                     ExceptionMessage, InnerException, StackTrace)
                    VALUES 
                    (@logDateTime, @logLevel, @applicationName, @source, @message,
                     @exceptionMessage, @innerException, @stackTrace)";

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@logDateTime", DateTime.UtcNow),
                    new SqlParameter("@logLevel", level),
                    new SqlParameter("@applicationName", _applicationName),
                    new SqlParameter("@source", _source),
                    new SqlParameter("@message", message ?? string.Empty),
                    new SqlParameter("@exceptionMessage", (object)exception?.Message ?? DBNull.Value),
                    new SqlParameter("@innerException", (object)exception?.InnerException?.Message ?? DBNull.Value),
                    new SqlParameter("@stackTrace", (object)exception?.StackTrace ?? DBNull.Value)
                };

                _eddsDbContext.ExecuteNonQuerySQLStatement(sql, parameters);
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, $"Failed to write to LTAS log: {ex.Message}");
            }
        }
    }
}