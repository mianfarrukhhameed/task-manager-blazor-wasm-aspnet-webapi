#nullable enable

using Fistix.TaskManager.AiLayer.Abstractions;
using Fistix.TaskManager.AiLayer.Implementations;
using Fistix.TaskManager.AiLayer.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Fistix.TaskManager.AiLayer.Tests;

public class EmbeddingPoolingTests
{
    [Fact]
    public void MeanPoolAndNormalize_AveragesActiveTokens_AndL2Normalizes()
    {
        // seq=3, hidden=2; mask drops middle token
        // tokens: [1,0], [9,9] (ignored), [0,1]
        float[] hidden =
        [
            1f, 0f,
            9f, 9f,
            0f, 1f
        ];
        long[] mask = [1, 0, 1];

        var pooled = EmbeddingPooling.MeanPoolAndNormalize(hidden, mask, sequenceLength: 3, hiddenSize: 2);

        Assert.Equal(2, pooled.Length);
        // Mean of [1,0] and [0,1] => [0.5, 0.5], then L2 => unit vector
        var expected = 1f / MathF.Sqrt(2f);
        Assert.Equal(expected, pooled[0], precision: 5);
        Assert.Equal(expected, pooled[1], precision: 5);

        var norm = MathF.Sqrt(pooled[0] * pooled[0] + pooled[1] * pooled[1]);
        Assert.Equal(1f, norm, precision: 5);
    }

    [Fact]
    public void L2NormalizeInPlace_ScalesToUnitLength()
    {
        Span<float> values = stackalloc float[] { 3f, 4f };
        EmbeddingPooling.L2NormalizeInPlace(values);
        Assert.Equal(0.6f, values[0], precision: 5);
        Assert.Equal(0.8f, values[1], precision: 5);
    }

    [Fact]
    public void ApplyInputKind_PrependsInstruction_ForQueryOnly()
    {
        const string instruction = "Represent this sentence for searching: ";
        var passage = EmbeddingPooling.ApplyInputKind("hello", EmbeddingInputKind.Passage, instruction);
        var query = EmbeddingPooling.ApplyInputKind("hello", EmbeddingInputKind.Query, instruction);
        var alreadyPrefixed = EmbeddingPooling.ApplyInputKind(
            instruction + "hello",
            EmbeddingInputKind.Query,
            instruction);

        Assert.Equal("hello", passage);
        Assert.Equal(instruction.TrimEnd() + " hello", query);
        Assert.Equal(instruction + "hello", alreadyPrefixed);
    }
}

public class OnnxBgeEmbeddingServiceIntegrationTests
{
    [Fact]
    public async Task GenerateEmbedding_WhenModelPresent_ReturnsUnit384Vector()
    {
        var modelDir = FindModelDirectory();
        if (modelDir is null)
        {
            // Optional integration: no weights checked in — skip without failing CI.
            return;
        }

        var aiConfig = new AiConfiguration
        {
            Embedding = new EmbeddingSettings
            {
                Provider = "Onnx",
                Model = "bge-small-en-v1.5",
                Dimension = 384,
                Onnx = new OnnxEmbeddingSettings
                {
                    ModelDirectory = modelDir,
                    MaxSequenceLength = 64,
                    QueryInstruction = "Represent this sentence for searching: "
                }
            }
        };

        using var service = new OnnxBgeEmbeddingService(aiConfig, NullLogger<OnnxBgeEmbeddingService>.Instance);

        var embedding = await service.GenerateEmbeddingAsync(
            "Ship the ONNX embedding pipeline",
            EmbeddingInputKind.Passage);

        Assert.Equal(384, embedding.Length);
        double sumSquares = 0;
        foreach (var v in embedding)
        {
            sumSquares += v * v;
        }

        Assert.InRange(Math.Sqrt(sumSquares), 0.99, 1.01);
    }

    private static string? FindModelDirectory()
    {
        var probe = new DirectoryInfo(AppContext.BaseDirectory);
        while (probe is not null)
        {
            var dir = Path.Combine(probe.FullName, "models", "bge-small-en-v1.5");
            if (File.Exists(Path.Combine(dir, "model.onnx")) && File.Exists(Path.Combine(dir, "vocab.txt")))
            {
                return dir;
            }

            probe = probe.Parent;
        }

        return null;
    }
}
