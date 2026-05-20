namespace Chatbot.Platforms.Waha;

public sealed class WahaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:3000";

    public string? ApiKey { get; set; }

    public string Session { get; set; } = "default";

    public int SendRetryCount { get; set; } = 3;
}
