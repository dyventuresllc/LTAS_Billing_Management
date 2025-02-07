using System;

namespace LTASBM.Agent.Logging
{
    public interface ILTASLogger
    {
        // Original methods
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogError(Exception ex, string message);
        void LogDebug(string message);

        // New overloads with formatting
        void LogInformation(string messageTemplate, params object[] propertyValues);
        void LogWarning(string messageTemplate, params object[] propertyValues);
        void LogError(string messageTemplate, params object[] propertyValues);
        void LogError(Exception ex, string messageTemplate, params object[] propertyValues);
        void LogDebug(string messageTemplate, params object[] propertyValues);
    }
}
