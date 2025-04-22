using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace EIDServiceWithSignalRClient;

internal class Program
{
    static async Task Main(string[] args)
    {
        // Build configuration to read appsettings.json
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        // Get hub URL from configuration; fallback to a default value
        var hubUrl = configuration["SignalR:HubUrl"] ?? "http://localhost:55455/notificationHub";

        // Create the SignalR connection
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        // Register a handler for the "ReceiveEvent" event that the service uses to send notifications
        connection.On<string, object>("ReceiveEvent", (eventType, eventData) =>
        {
            Console.WriteLine($"Received event of type '{eventType}': {eventData}");
        });

        try
        {
            // Start the connection
            await connection.StartAsync();
            Console.WriteLine($"Connected to SignalR hub at {hubUrl}");

            // After connecting, register for events by invoking the RegisterForEvents method on the hub.
            // The event type (group name) should match what your service expects.
            await connection.InvokeAsync("RegisterForEvents", "DataReceived");
            Console.WriteLine("Registered for event 'DataReceived'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting or registering: {ex.Message}");
            return;
        }

        // Keep the client running until the user presses a key.
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        // Stop the connection gracefully
        await connection.StopAsync();
        Console.WriteLine("Connection stopped. Exiting...");
    }
}
