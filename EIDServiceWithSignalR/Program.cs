using EIDServiceWithSignalR.Hubs;
using EIDServiceWithSignalR.Workers;
using Microsoft.Extensions.Hosting.WindowsServices;
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
        builder.Services.AddSignalR();
        builder.Services.AddHostedService<SerialWorker>();
        //services.AddHttpClient();
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