#nullable enable

using FastBertTokenizer;
using Fistix.TaskManager.AiLayer.Abstractions;
using Fistix.TaskManager.AiLayer.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Fistix.TaskManager.AiLayer.Implementations;

/// <summary>
/// Local BGE-small embeddings via ONNX Runtime (mean-pool + L2 normalize).
/// Registered as a singleton — InferenceSession creation is expensive.
/// Model files are loaded lazily on first embed so startup succeeds when embeddings are off.
/// </summary>
public sealed class OnnxBgeEmbeddingService : IEmbeddingService, IDisposable
{
    private readonly AiConfiguration _aiConfig;
    private readonly ILogger<OnnxBgeEmbeddingService> _logger;
    private readonly object _gate = new();
    private readonly Lazy<(InferenceSession Session, BertTokenizer Tokenizer, int MaxSequenceLength)> _runtime;
    private bool _disposed;

    public OnnxBgeEmbeddingService(
        AiConfiguration aiConfig,
        ILogger<OnnxBgeEmbeddingService> logger)
    {
        _aiConfig = aiConfig ?? throw new ArgumentNullException(nameof(aiConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtime = new Lazy<(InferenceSession, BertTokenizer, int)>(CreateRuntime, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string ModelName => string.IsNullOrWhiteSpace(_aiConfig.Embedding.Model)
        ? "bge-small-en-v1.5"
        : _aiConfig.Embedding.Model;

    public int Dimension => _aiConfig.Embedding.Dimension;

    public Task<float[]> GenerateEmbeddingAsync(
        string text,
        EmbeddingInputKind kind = EmbeddingInputKind.Passage,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text is required to generate an embedding.", nameof(text));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var prepared = EmbeddingPooling.ApplyInputKind(
            text.Trim(),
            kind,
            _aiConfig.Embedding.Onnx?.QueryInstruction);

        float[] embedding;
        lock (_gate)
        {
            embedding = EmbedUnsafe(prepared);
        }

        if (embedding.Length != Dimension)
        {
            throw new InvalidOperationException(
                $"ONNX embedding dimension {embedding.Length} does not match configured {Dimension}.");
        }

        return Task.FromResult(embedding);
    }

    private (InferenceSession Session, BertTokenizer Tokenizer, int MaxSequenceLength) CreateRuntime()
    {
        var onnx = _aiConfig.Embedding.Onnx ?? new OnnxEmbeddingSettings();
        var maxSequenceLength = Math.Max(8, onnx.MaxSequenceLength);
        var modelDir = ResolveModelDirectory(onnx.ModelDirectory);
        var modelPath = Path.Combine(modelDir, "model.onnx");
        var vocabPath = Path.Combine(modelDir, "vocab.txt");

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException(
                $"ONNX embedding model not found at '{modelPath}'. Run scripts/download-bge-onnx.sh first.",
                modelPath);
        }

        if (!File.Exists(vocabPath))
        {
            throw new FileNotFoundException(
                $"ONNX embedding vocab not found at '{vocabPath}'. Run scripts/download-bge-onnx.sh first.",
                vocabPath);
        }

        var session = new InferenceSession(modelPath);
        var tokenizer = new BertTokenizer();
        using (var reader = File.OpenText(vocabPath))
        {
            tokenizer.LoadVocabulary(reader, convertInputToLowercase: true);
        }

        _logger.LogInformation(
            "Loaded ONNX BGE embedding model from {ModelDirectory} (dim={Dimension}, maxSeq={MaxSeq})",
            modelDir,
            Dimension,
            maxSequenceLength);

        return (session, tokenizer, maxSequenceLength);
    }

    private float[] EmbedUnsafe(string text)
    {
        var (session, tokenizer, maxSequenceLength) = _runtime.Value;

        var (inputIdsMem, attentionMaskMem, tokenTypeIdsMem) =
            tokenizer.Encode(text, maxSequenceLength, padTo: maxSequenceLength);

        var seqLen = inputIdsMem.Length;
        var inputIds = inputIdsMem.ToArray();
        var attentionMask = attentionMaskMem.ToArray();
        var tokenTypeIds = tokenTypeIdsMem.Length == seqLen
            ? tokenTypeIdsMem.ToArray()
            : new long[seqLen];

        var inputIdsTensor = new DenseTensor<long>(inputIds, [1, seqLen]);
        var attentionMaskTensor = new DenseTensor<long>(attentionMask, [1, seqLen]);
        var tokenTypeIdsTensor = new DenseTensor<long>(tokenTypeIds, [1, seqLen]);

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor)
        };

        if (session.InputMetadata.ContainsKey("token_type_ids"))
        {
            inputs.Add(NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIdsTensor));
        }

        using var results = session.Run(inputs);
        var output = results.First();
        var tensor = output.AsTensor<float>();
        var dims = tensor.Dimensions.ToArray();
        var flat = tensor.ToArray();

        // Already-pooled sentence embedding: [batch, hidden] or [hidden]
        if (dims.Length == 1 && dims[0] == Dimension)
        {
            var pooled = flat.ToArray();
            EmbeddingPooling.L2NormalizeInPlace(pooled);
            return pooled;
        }

        if (dims.Length == 2 && dims[0] == 1 && dims[1] == Dimension)
        {
            var pooled = flat.ToArray();
            EmbeddingPooling.L2NormalizeInPlace(pooled);
            return pooled;
        }

        // Token embeddings: [batch, seq, hidden] or [seq, hidden]
        int sequenceLength;
        int hiddenSize;

        if (dims.Length == 3)
        {
            sequenceLength = dims[1];
            hiddenSize = dims[2];
        }
        else if (dims.Length == 2)
        {
            sequenceLength = dims[0];
            hiddenSize = dims[1];
        }
        else
        {
            throw new InvalidOperationException(
                $"Unexpected ONNX output shape [{string.Join(", ", dims)}]; expected token or sentence embeddings.");
        }

        return EmbeddingPooling.MeanPoolAndNormalize(
            flat,
            attentionMask.AsSpan(0, Math.Min(sequenceLength, attentionMask.Length)),
            sequenceLength,
            hiddenSize);
    }

    internal static string ResolveModelDirectory(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath) && Directory.Exists(configuredPath))
        {
            return configuredPath;
        }

        var searchRoots = new List<string>
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        // Walk up from cwd and base dir looking for repo-root models/
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var probe = new DirectoryInfo(start);
            for (var i = 0; i < 6 && probe is not null; i++)
            {
                searchRoots.Add(probe.FullName);
                probe = probe.Parent;
            }
        }

        foreach (var root in searchRoots.Distinct(StringComparer.Ordinal))
        {
            var candidate = Path.GetFullPath(Path.Combine(root, configuredPath));
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "model.onnx")))
            {
                return candidate;
            }
        }

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredPath));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_runtime.IsValueCreated)
        {
            _runtime.Value.Session.Dispose();
        }
    }
}
