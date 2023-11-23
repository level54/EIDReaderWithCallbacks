namespace EIDWebAPI;

public class Dispatcher : IDispatcher
{
    private readonly ILogger<Dispatcher> _logger;

    public Dispatcher(ILogger<Dispatcher> logger)
    {
        _logger = logger;
    }

    public event EventHandler<EventReceivedArgs>? EventReceived;

    public void OnEventReceived(EventReceivedArgs args)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Event Received : {EventText}", args.EventBody.Substring(0, args.EventBody.Length > 50 ? 50 : args.EventBody.Length));
        }

        EventReceived?.Invoke(this, args);
    }
}

public class EventReceivedArgs : EventArgs
{
    public EventReceivedArgs(string eventBody)
    {
        EventBody = eventBody;
    }

    public string EventBody { get; }
}