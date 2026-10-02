namespace TelegramBot.Models;

public class TelegramOptions
{
    public string BotToken { get; set; } = null!;
    public string Currency { get; set; } = "AMD";
    public int DigestHourUtc { get; set; } = 18;
    public string Mode { get; set; } = "LongPolling";
    public string WebhookSecret { get; set; } = "";
    public string PublicBaseUrl { get; set; } = "http://localhost:5080";
}
