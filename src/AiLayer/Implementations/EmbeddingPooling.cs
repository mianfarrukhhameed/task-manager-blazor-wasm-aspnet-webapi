#nullable enable

using Fistix.TaskManager.AiLayer.Abstractions;

namespace Fistix.TaskManager.AiLayer.Implementations;

/// <summary>
/// Mean pooling + L2 normalization helpers for transformer last-hidden-state embeddings.
/// </summary>
public static class EmbeddingPooling
{
    /// <summary>
    /// Mean-pools a [seqLen, hidden] row-major tensor using an attention mask, then L2-normalizes.
    /// </summary>
    public static float[] MeanPoolAndNormalize(
        ReadOnlySpan<float> lastHiddenState,
        ReadOnlySpan<long> attentionMask,
        int sequenceLength,
        int hiddenSize)
    {
        if (sequenceLength <= 0 || hiddenSize <= 0)
        {
            throw new ArgumentException("sequenceLength and hiddenSize must be positive.");
        }

        if (lastHiddenState.Length < sequenceLength * hiddenSize)
        {
            throw new ArgumentException(
                $"lastHiddenState length {lastHiddenState.Length} is smaller than sequenceLength*hiddenSize ({sequenceLength * hiddenSize}).");
        }

        if (attentionMask.Length < sequenceLength)
        {
            throw new ArgumentException(
                $"attentionMask length {attentionMask.Length} is smaller than sequenceLength ({sequenceLength}).");
        }

        var pooled = new float[hiddenSize];
        var tokenCount = 0f;

        for (var t = 0; t < sequenceLength; t++)
        {
            if (attentionMask[t] == 0)
            {
                continue;
            }

            tokenCount += 1f;
            var offset = t * hiddenSize;
            for (var h = 0; h < hiddenSize; h++)
            {
                pooled[h] += lastHiddenState[offset + h];
            }
        }

        if (tokenCount <= 0f)
        {
            throw new InvalidOperationException("Cannot pool embedding: attention mask has no active tokens.");
        }

        for (var h = 0; h < hiddenSize; h++)
        {
            pooled[h] /= tokenCount;
        }

        L2NormalizeInPlace(pooled);
        return pooled;
    }

    public static void L2NormalizeInPlace(Span<float> values)
    {
        double sumSquares = 0;
        for (var i = 0; i < values.Length; i++)
        {
            sumSquares += values[i] * (double)values[i];
        }

        var norm = Math.Sqrt(sumSquares);
        if (norm < 1e-12)
        {
            return;
        }

        var scale = (float)(1.0 / norm);
        for (var i = 0; i < values.Length; i++)
        {
            values[i] *= scale;
        }
    }

    public static string ApplyInputKind(string text, EmbeddingInputKind kind, string? queryInstruction)
    {
        if (kind != EmbeddingInputKind.Query || string.IsNullOrWhiteSpace(queryInstruction))
        {
            return text;
        }

        var instruction = queryInstruction.TrimEnd();
        if (text.StartsWith(instruction, StringComparison.Ordinal))
        {
            return text;
        }

        return $"{instruction} {text}";
    }
}
