namespace Fistix.TaskManager.AiLayer.Shared;

public static class PromptInputSanitizer
{
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        return input
            .Replace("<!--", string.Empty, StringComparison.Ordinal)
            .Replace("-->", string.Empty, StringComparison.Ordinal)
            .Replace("{", "{{", StringComparison.Ordinal)
            .Replace("}", "}}", StringComparison.Ordinal);
    }
}
