using Microsoft.Extensions.Options;
using Telegram.Bot;
using TelegramBot.Models;

namespace TelegramBot.Services;

public class TelegramWebhookWorker : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly TelegramOptions _options;

    public TelegramWebhookWorker(
        ITelegramBotClient botClient,
        IOptions<TelegramOptions> options)
    {
        _botClient = botClient;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!string.Equals(
                _options.Mode,
                "Webhook",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            throw new InvalidOperationException(
                "PublicBaseUrl is required for Webhook mode.");

        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
            throw new InvalidOperationException(
                "WebhookSecret is required for Webhook mode.");

        var webhookUrl =
            $"{_options.PublicBaseUrl.TrimEnd('/')}/telegram/webhook";

        await _botClient.SetWebhook(
            url: webhookUrl,
            secretToken: _options.WebhookSecret,
            dropPendingUpdates: false,
            cancellationToken: stoppingToken);

        await Task.Delay(
            Timeout.Infinite,
            stoppingToken);
    }

    public override async Task StopAsync(
        CancellationToken cancellationToken)
    {
        if (string.Equals(
                _options.Mode,
                "Webhook",
                StringComparison.OrdinalIgnoreCase))
        {
            await _botClient.DeleteWebhook(
                dropPendingUpdates: false,
                cancellationToken: cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }
}
