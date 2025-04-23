using EIDServiceWithSignalR.Hubs;
using EIDServiceWithSignalR.Workers;
using Microsoft.Extensions.Hosting.WindowsServices;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace EIDServiceWithSignalR;

public class Program
{
    public static void Main(string[] args)
    {
        //create the required options to run the app as a windows service
        var options = new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
        };
        var builder = WebApplication.CreateBuilder(options);

        //build the windows service
        builder.Host.UseWindowsService(options =>
        {
            options.ServiceName = "EIDServiceWithSignalR";
        });

        //build the logger
        builder.Host.UseSerilog((context, config) =>
        {
            config.ReadFrom.Configuration(context.Configuration);
        });

        //configure services
        builder.Services.AddSignalR()
        //.AddNewtonsoftJsonProtocol();
            .AddNewtonsoftJsonProtocol(options =>
            {
                // Optionally configure Newtonsoft.Json settings.
                //options.PayloadSerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.PayloadSerializerSettings.ContractResolver = new DefaultContractResolver();
                options.PayloadSerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
            });

        builder.Services.AddHostedService<SerialWorker>();
        builder.Services.AddSingleton<IEventNotificationService, EventNotificationService>();

        //build the app
        var app = builder.Build();

        //enable developer pages as needed
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        //enable routing
        app.UseRouting();

        //map the hubs
        app.MapHub<NotificationHub>("/notificationHub");

        app.Run();
    }
}