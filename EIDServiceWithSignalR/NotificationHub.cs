// Hubs/NotificationHub.cs
using Microsoft.AspNetCore.SignalR;

namespace EIDServiceWithSignalR.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task RegisterForEvents(string eventType)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, eventType);
            _logger.LogInformation("Client {ConnectionId} registered for event: {EventType}", Context.ConnectionId, eventType);
        }
    }

    // Services/IEventNotificationService.cs
    public interface IEventNotificationService
    {
        Task NotifyClientsOfSerialDataReceivedAsync(string eventType, object data);
        Task NotifyClientsAsync(string eventType, object data);
    }

    // Services/EventNotificationService.cs
    public class EventNotificationService : IEventNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<EventNotificationService> _logger;

        public EventNotificationService(
            IHubContext<NotificationHub> hubContext,
            ILogger<EventNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyClientsAsync(string eventType, object data)
        {
            _logger.LogInformation($"Sending notification for event: {eventType}, using method NotifyClients");
            await _hubContext.Clients.Group(eventType).SendAsync("NotifyClients", eventType, data);
        }

        public async Task NotifyClientsOfSerialDataReceivedAsync(string eventType, object data)
        {
            _logger.LogInformation($"Sending notification for event: {eventType}, using method SerialDataReceivedEvent");
            await _hubContext.Clients.Group(eventType).SendAsync("SerialDataReceivedEvent", eventType, data);
        }
    }
}