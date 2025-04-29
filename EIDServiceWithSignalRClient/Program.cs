using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

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
            //.AddNewtonsoftJsonProtocol()
            .AddNewtonsoftJsonProtocol(options =>
            {
                // Ensure the client-side matches the server's configuration if needed.
                //options.PayloadSerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.PayloadSerializerSettings.ContractResolver = new DefaultContractResolver();
                options.PayloadSerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
            })
            .WithAutomaticReconnect()
            .Build();

        // Register a handler for the "SerialDataReceivedEvent" event that the service uses to send notifications
        connection.On<string, JObject>("SerialDataReceivedEvent", (eventType, eventData) =>
        {
            switch (eventType)
            {
                case "DataReceived":
                    {
                        // Extract the Schema and Data parts from the payload
                        var schemaToken = eventData["Schema"];
                        var dataToken = eventData["Data"];
                        if (schemaToken == null || dataToken == null)
                        {
                            Console.WriteLine("Payload is missing 'Schema' or 'Data' properties.");
                            return;
                        }

                        string schemaJson = schemaToken.ToString();
                        string dataJson = dataToken.ToString();

                        // Build the dynamic object that will have properties as defined in the schema,
                        // with values from the data JSON.
                        dynamic dynamicObj = DynamicObjectBuilder.BuildDynamicObject(schemaJson, dataJson);

                        // Now you can access properties by name on the dynamic object
                        // For example, if your schema includes "TimeStamp" and "Jig":
                        Console.WriteLine($"TimeStamp: {dynamicObj.TimeStamp}");
                        Console.WriteLine($"Name: {dynamicObj.Name}");
                        // If there are more properties, you can iterate through them:
                        Console.WriteLine("\nAll properties in dynamic object:");
                        foreach (var property in (IDictionary<string, object>)dynamicObj)
                        {
                            Console.WriteLine($"  {property.Key} : {property.Value}");
                        }
                    }
                    break;
                default:
                    break;
            }

            //Console.WriteLine($"Received event of type '{eventType}': {eventData}");
        });
        connection.On<string, object>("NotifyClients", HandleNotifyClientsEvent);

        try
        {
            // Start the connection
            await connection.StartAsync();
            Console.WriteLine($"Connected to SignalR hub at {hubUrl}");

            // After connecting, register for events by invoking the RegisterForEvents method on the hub.
            // The event type (group name) should match what your service expects.
            await connection.InvokeAsync("RegisterForEvents", "DataReceived");
            Console.WriteLine("Registered for event 'DataReceived'.");
            await connection.InvokeAsync("RegisterForEvents", "ThreadSleep");
            Console.WriteLine("Registered for event 'ThreadSleep'.");
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

    private static void HandleNotifyClientsEvent(string eventName, object data) 
    {
        Console.WriteLine($"{eventName} : {data}");
    }
}
