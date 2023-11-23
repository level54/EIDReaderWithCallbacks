using EIDWebAPI.Models;
using System.Text;

namespace EIDWebAPI.Classes;

public class CallbackManager
{
    private readonly List<string> _callbackUrls = new List<string>();
    private readonly ILogger<CallbackManager> _logger;

    public CallbackManager(ILogger<CallbackManager> logger)
    {
        _logger = logger;
    }

    public void RegisterCallback(string callbackUrl)
    {
        _callbackUrls.Add(callbackUrl);
    }

    public void UnregisterCallback(string callbackUrl)
    {
        _callbackUrls.Remove(callbackUrl);
    }

    public async void InvokeCallbacksAsync(string message)
    {
        using var httpClient = new HttpClient();
        var eventData = new { EventData = message };

        var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(eventData),
                                        Encoding.UTF8,
                                        "application/json");

        foreach (var callbackUrl in _callbackUrls)
        {
            try
            {
                await httpClient.PostAsync(callbackUrl, content);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception logging to - {callbackUrl}", ex);
            }
        }
    }
}
