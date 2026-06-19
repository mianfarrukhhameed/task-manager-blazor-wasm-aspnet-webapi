namespace Fistix.TaskManager.AiLayer.Shared;

public class AiRateLimitConfiguration
{
    public bool Enabled { get; set; } = true;
    public int PermitLimit { get; set; } = 1;
    public int WindowMinutes { get; set; } = 1;
}
