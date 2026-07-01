using System.Threading.Channels;

namespace GitBuddy.Api.Services;

public interface IBranchWithoutPRRefreshTrigger
{
    void Trigger();

    ChannelReader<object> Reader { get; }
}

public class BranchWithoutPRRefreshTrigger : IBranchWithoutPRRefreshTrigger
{
    // Bounded to 1 with DropWrite: if a trigger is already pending, additional
    // Trigger() calls are silently dropped. This gives idempotent manual refresh
    // without touching ChannelReader.Count (which throws on unbounded channels
    // and would race with the worker's drain loop).
    private readonly Channel<object> _channel =
        Channel.CreateBounded<object>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true
        });

    public ChannelReader<object> Reader => _channel.Reader;

    public void Trigger() => _channel.Writer.TryWrite(new object());
}
