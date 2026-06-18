namespace Fistix.TaskManager.AiLayer.Shared;

/// <summary>
/// Input limits for LLM prompts. Mirrors ViewModel TodoFieldLimits values.
/// </summary>
public static class LlmInputLimits
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 4000;
    public const int SummaryMaxLength = 500;
}
