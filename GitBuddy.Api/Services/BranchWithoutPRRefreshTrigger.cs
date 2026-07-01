using System.Threading.Channels;

namespace GitBuddy.Api.Services;

public interface IBranchWithoutPRRefreshTrigger
{
    void Trigger();

    ChannelReader<object> Reader { get; }
}

public class BranchWithoutPRRefreshTrigger : IBranchWithoutPRRefreshTrigger
{
    private readonly Channel<object> _channel =
        Channel.CreateUnbounded<object>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<object> Reader => _channel.Reader;

    public void Trigger()
    {
        if (_channel.Reader.Count == 0)
        {
            _channel.Writer.TryWrite(new object());
        }
    }
}
