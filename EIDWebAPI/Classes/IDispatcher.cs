namespace EIDWebAPI;

public interface IDispatcher
{
    event EventHandler<EventReceivedArgs>? EventReceived;

    void OnEventReceived(EventReceivedArgs args);
}