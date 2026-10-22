using Nexora.Infrastructure.BackgroundProcessing;

namespace Nexora.Tests.Application;

public sealed class DocumentProcessingControlTests
{
    [Fact]
    public void Cancelling_one_version_does_not_cancel_another()
    {
        var queue = new DocumentProcessingQueue();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        using var first = queue.Begin(firstId, CancellationToken.None);
        using var second = queue.Begin(secondId, CancellationToken.None);

        Assert.True(queue.Cancel(firstId));
        Assert.True(first.IsCancellationRequested);
        Assert.False(second.IsCancellationRequested);
        Assert.False(queue.Cancel(Guid.NewGuid()));

        queue.End(firstId);
        queue.End(secondId);
    }
}
