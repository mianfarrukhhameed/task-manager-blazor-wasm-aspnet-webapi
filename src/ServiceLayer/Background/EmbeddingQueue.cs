#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Fistix.TaskManager.ServiceLayer.Background;

public interface IEmbeddingQueue
{
    ValueTask EnqueueAsync(Guid todoExternalId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken);
}

public sealed class EmbeddingQueue : IEmbeddingQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(Guid todoExternalId, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(todoExternalId, cancellationToken);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
