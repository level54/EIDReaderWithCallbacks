using CallbackAPI.Models;
using CallbackAPI.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CallbackAPI.Controllers;
[Route("api/[controller]")]
[ApiController]

[Produces("application/json")]
public class CallbackController : ControllerBase
{
    private readonly ILogger<CallbackController> _logger;
    private readonly IConfiguration _config;

    public CallbackController(ILogger<CallbackController> logger,
                              IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    [HttpPost("RegisterMeForCallback")]
    public async Task<IActionResult> RegisterMe()
    {
        var apiUrl = $"{_config.GetConnectionString("EIDWebAPI")}/api/Event/RegisterCallback";

        string callbackUrl = "http://localhost:44444/api/Callback/ReceiveCallback";

        var callbackModel = new { CallbackUrl = callbackUrl };

        using (var httpClient = new HttpClient())
        {
            var content = new StringContent(
                Newtonsoft.Json.JsonConvert.SerializeObject(callbackModel),
                Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Callback registered successfully!");
                return Ok(response);
            }
            else
            {
                _logger.LogInformation($"Failed to register callback. Status code: {response.StatusCode}");
                return Problem(detail: response.StatusCode.ToString());
            }
        }
    }

    [HttpPost("DeregisterMeForCallback")]
    public async Task<IActionResult> DeregisterMe()
    {
        var apiUrl = $"{_config.GetConnectionString("EIDWebAPI")}/api/Event/RemoveCallback";

        string callbackUrl = "http://localhost:44444/api/Callback/ReceiveCallback";

        var callbackModel = new { CallbackUrl = callbackUrl };

        using (var httpClient = new HttpClient())
        {
            var content = new StringContent(
                Newtonsoft.Json.JsonConvert.SerializeObject(callbackModel),
                Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Callback deregistered successfully!");
                return Ok(response);
            }
            else
            {
                _logger.LogInformation($"Failed to deregister callback. Status code: {response.StatusCode}");
                return Problem(detail: response.StatusCode.ToString());
            }
        }
    }

    [HttpPost("ReceiveCallback")]
    //[ApiExplorerSettings(IgnoreApi = true)] // This line hides the action from Swagger
    public IActionResult ReceiveCallback([FromBody] EventDataModel eventData)
    {
        // Process the callback data received from the API
        // You can perform any necessary logic with the received data

        // For example, log the event data
        _logger.LogInformation($"Received callback with data: {eventData.EventData}");

        try
        {
            var output = _config.GetSection("OutputFile").Get<OutputFileSettings>();

            if (output != null && output.Name != null)
            {
                System.IO.File.AppendAllText(output.Name, $"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")} - {eventData.EventData}\r\n");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }

        // You can also return a response if needed
        return Ok("Callback received successfully");
    }
}
