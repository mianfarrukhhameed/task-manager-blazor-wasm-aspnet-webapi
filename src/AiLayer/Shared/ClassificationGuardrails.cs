using System;
using System.Linq;

namespace Fistix.TaskManager.AiLayer.Shared;

public static class ClassificationGuardrails
{
    private static readonly string[] BlockerKeywords =
    [
        "production down",
        "cannot login",
        "can't login",
        "payment failed",
        "site is down",
        "outage"
    ];

    private static readonly string[] ChoreKeywords =
    [
        "documentation",
        "docs",
        "cleanup",
        "housekeeping",
        "organize",
        "review notes"
    ];

    public static (string Priority, float Confidence, string? Reason) Apply(
        string suggestedPriority,
        float confidence,
        string? reason,
        string title,
        string description,
        DateTime? dueDate)
    {
        var text = $"{title} {description}".ToLowerInvariant();
        var adjustedReason = reason;

        if (BlockerKeywords.Any(keyword => text.Contains(keyword, StringComparison.Ordinal)))
        {
            if (!IsAtLeastHigh(suggestedPriority))
            {
                adjustedReason = "Guardrail: critical blocker keywords detected.";
            }

            suggestedPriority = "HIGH";
            confidence = Math.Max(confidence, 0.85f);
        }

        if (dueDate is null && ChoreKeywords.Any(keyword => text.Contains(keyword, StringComparison.Ordinal)))
        {
            if (IsHigh(suggestedPriority))
            {
                suggestedPriority = "MEDIUM";
                adjustedReason = "Guardrail: informational/chore task capped at MEDIUM.";
                confidence = Math.Min(confidence, 0.84f);
            }
        }

        return (NormalizePriority(suggestedPriority), ClampConfidence(confidence), adjustedReason);
    }

    public static string NormalizePriority(string? priority)
    {
        return priority?.Trim().ToUpperInvariant() switch
        {
            "HIGH" => "HIGH",
            "LOW" => "LOW",
            _ => "MEDIUM"
        };
    }

    public static string ToTaskPriority(string? priority) =>
        NormalizePriority(priority) switch
        {
            "HIGH" => "High",
            "LOW" => "Low",
            _ => "Medium"
        };

    /// <summary>
    /// True when the task priority differs from the AI suggestion (any casing / format).
    /// </summary>
    public static bool IsPriorityOverridden(string? taskPriority, string? aiPriority) =>
        !string.Equals(
            NormalizePriority(taskPriority),
            NormalizePriority(aiPriority),
            StringComparison.Ordinal);

    private static bool IsHigh(string priority) =>
        string.Equals(priority, "HIGH", StringComparison.OrdinalIgnoreCase);

    private static bool IsAtLeastHigh(string priority) => IsHigh(priority);

    private static float ClampConfidence(float confidence) =>
        Math.Clamp(confidence, 0f, 1f);
}
