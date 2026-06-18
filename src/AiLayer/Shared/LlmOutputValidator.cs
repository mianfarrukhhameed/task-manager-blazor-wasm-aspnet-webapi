using System;

namespace Fistix.TaskManager.AiLayer.Shared;

public static class LlmOutputValidator
{
    public static string ValidateSummary(string? raw, int maxLength = LlmInputLimits.SummaryMaxLength)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException("AI returned an empty summary.");
        }

        return raw.Trim().Length <= maxLength
            ? raw.Trim()
            : raw.Trim()[..maxLength];
    }
}
