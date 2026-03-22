using MusicGrabber.Shared.Events;

namespace MusicGrabber.Shared.UnitTests;

public class InProcessEventBusTests
{
    [Fact]
    public async Task PublishAsync_WithSubscriber_InvokesHandler()
    {
        var bus = new InProcessEventBus();
        DownloadCompletedEvent? received = null;
        bus.Subscribe<DownloadCompletedEvent>((e, _) =>
        {
            received = e;
            return Task.CompletedTask;
        });

        var evt = new DownloadCompletedEvent(Guid.NewGuid(), "user1", 1024);
        await bus.PublishAsync(evt);

        received.Should().NotBeNull();
        received!.UserId.Should().Be("user1");
    }

    [Fact]
    public async Task PublishAsync_WithMultipleSubscribers_InvokesAll()
    {
        var bus = new InProcessEventBus();
        var count = 0;
        bus.Subscribe<DownloadCompletedEvent>((_, _) => { count++; return Task.CompletedTask; });
        bus.Subscribe<DownloadCompletedEvent>((_, _) => { count++; return Task.CompletedTask; });

        await bus.PublishAsync(new DownloadCompletedEvent(Guid.NewGuid(), "user1", 1024));

        count.Should().Be(2);
    }

    [Fact]
    public async Task PublishAsync_WithNoSubscribers_DoesNotThrow()
    {
        var bus = new InProcessEventBus();

        Func<Task> act = () => bus.PublishAsync(new DownloadCompletedEvent(Guid.NewGuid(), "user1", 1024));

        await act.Should().NotThrowAsync();
    }
}
