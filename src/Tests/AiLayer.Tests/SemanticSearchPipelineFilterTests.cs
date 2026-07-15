#nullable enable

using Fistix.TaskManager.AiLayer.Abstractions;
using Fistix.TaskManager.AiLayer.Implementations;
using Xunit;

namespace Fistix.TaskManager.AiLayer.Tests;

public class SemanticSearchPipelineFilterTests
{
    [Fact]
    public void FilterByMinSimilarity_DropsWeakHits()
    {
        var hits = new[]
        {
            new VectorSearchHit(Guid.NewGuid(), 1, 0.72),
            new VectorSearchHit(Guid.NewGuid(), 2, 0.334),
            new VectorSearchHit(Guid.NewGuid(), 3, 0.45)
        };

        var filtered = SemanticSearchPipeline.FilterByMinSimilarity(hits, minSimilarity: 0.45);

        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, h => Assert.True(h.Similarity >= 0.45));
    }

    [Fact]
    public void FilterByMinSimilarity_ReturnsEmpty_WhenAllBelowThreshold()
    {
        var hits = new[]
        {
            new VectorSearchHit(Guid.NewGuid(), 1, 0.33)
        };

        var filtered = SemanticSearchPipeline.FilterByMinSimilarity(hits, minSimilarity: 0.45);

        Assert.Empty(filtered);
    }
}
