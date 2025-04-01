using Asp.Versioning;
using EIDWebAPI.Classes;
using EIDWebAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EIDWebAPI.Controllers;
[Route("api/[controller]")]
[ApiController]

[Produces("application/json")]
public class EventController : ControllerBase
{
    private readonly ILogger<EventController> _logger;
    private readonly IDispatcher _dispatcher;
    private readonly CallbackManager _callbackManager;

    public EventController(ILogger<EventController> logger,
                           IDispatcher dispatcher,
                           CallbackManager callbackManager)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _callbackManager = callbackManager;

        //attach this controller to the dispatcher's EventReceived event
        _dispatcher.EventReceived += _dispatcher_EventReceived;
    }

    private void _dispatcher_EventReceived(object? sender, EventReceivedArgs e)
    {
        //call the callbacks here
        //https://stackoverflow.com/questions/58387950/how-to-handle-api-callbacks-in-asp-net-mvc-helloworks-api-in-my-case
        //https://www.c-sharpcorner.com/forums/call-api-from-mvc-with-post-data-having-callback-url-asp-net-mvc

        _callbackManager.InvokeCallbacksAsync(e.EventBody);
    }

    [HttpPost("RegisterCallback")]
    //[ApiExplorerSettings(IgnoreApi = true)] // This line hides the action from Swagger
    public IActionResult RegisterCallback([FromBody] CallbackModel callbackModel)
    {
        _logger.LogInformation($"Register Callback : {callbackModel.CallbackUrl}");
        _callbackManager.RegisterCallback(callbackModel.CallbackUrl ?? string.Empty);
        return Ok();
    }

    [HttpPost("RemoveCallback")]
    //[ApiExplorerSettings(IgnoreApi = true)] // This line hides the action from Swagger
    public IActionResult RemoveCallback([FromBody] CallbackModel callbackModel)
    {
        _logger.LogInformation($"Deregister Callback : {callbackModel.CallbackUrl}");
        _callbackManager.UnregisterCallback(callbackModel.CallbackUrl ?? string.Empty);
        return Ok();
    }

}
