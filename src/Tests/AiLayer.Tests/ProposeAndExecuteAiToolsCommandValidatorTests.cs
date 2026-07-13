using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Validators.Todos;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Fistix.TaskManager.AiLayer.Tests;

public class ProposeAiToolsCommandValidatorTests
{
    private readonly ProposeAiToolsCommandValidator _validator = new();

    [Fact]
    public void AcceptsValidPrompt()
    {
        var result = _validator.Validate(new ProposeAiToolsCommand
        {
            Prompt = "Create three tasks for Q2 planning"
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void RejectsEmptyPrompt()
    {
        var result = _validator.Validate(new ProposeAiToolsCommand { Prompt = "" });
        Assert.False(result.IsValid);
    }
}

public class ExecuteAiToolsCommandValidatorTests
{
    private readonly ExecuteAiToolsCommandValidator _validator = new();

    [Fact]
    public void AcceptsValidConfirmedCalls()
    {
        var result = _validator.Validate(new ExecuteAiToolsCommand
        {
            ConfirmedCalls =
            [
                new ProposedToolCallDto
                {
                    ToolName = "create_todo",
                    Arguments = new Dictionary<string, JsonElement>
                    {
                        ["title"] = JsonSerializer.SerializeToElement("Plan OKRs")
                    }
                }
            ]
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void RejectsEmptyConfirmedCalls()
    {
        var result = _validator.Validate(new ExecuteAiToolsCommand { ConfirmedCalls = [] });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void RejectsUnknownToolName()
    {
        var result = _validator.Validate(new ExecuteAiToolsCommand
        {
            ConfirmedCalls =
            [
                new ProposedToolCallDto
                {
                    ToolName = "delete_everything",
                    Arguments = new Dictionary<string, JsonElement>()
                }
            ]
        });
        Assert.False(result.IsValid);
    }
}
