using Serilog;
using Microsoft.Extensions.Hosting.WindowsServices;
using EIDWebAPI.Workers;
using EIDWebAPI.Classes;
using CallbackAPI.Settings;
using Asp.Versioning.ApiExplorer;

namespace EIDWebAPI;

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

        // Add services to the container.
        builder.Services.AddControllers();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        //build the windows service
        builder.Host.UseWindowsService(options =>
        {
            options.ServiceName = "EIDWebAPI";
        });

        //build the logger
        builder.Host.UseSerilog((context, config) =>
        {
            config.ReadFrom.Configuration(context.Configuration);
        });

        //register the service for interclass communication
        builder.Services.AddSingleton<IDispatcher, Dispatcher>();
        builder.Services.AddSingleton<CallbackManager>();

        //add the hosted services here
        builder.Services.AddHostedService<SerialWorker>();

        var app = builder.Build();

        var swaggerSettings = app.Configuration.GetSection("Swagger").Get<SwaggerSettings>();

        // Configure the HTTP request pipeline.
        if ((bool)(swaggerSettings?.Enabled))
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();

        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
