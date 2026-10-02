using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using TelegramBot.Models;
using TelegramBot.Services;

namespace TelegramBot;

public class TelegramPollingWorker : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly TelegramUpdateHandler _updateHandler;
    private readonly TelegramOptions _options;

    public TelegramPollingWorker(
        ITelegramBotClient botClient,
        TelegramUpdateHandler updateHandler,
        IOptions<TelegramOptions> options)
    {
        _botClient = botClient;
        _updateHandler = updateHandler;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!string.Equals(
                _options.Mode,
                "LongPolling",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await _botClient.DeleteWebhook(
            dropPendingUpdates: false,
            cancellationToken: stoppingToken);

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            cancellationToken: stoppingToken);

        await Task.Delay(
            Timeout.Infinite,
            stoppingToken);
    }

    private async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        await _updateHandler.HandleAsync(
            update,
            cancellationToken);
    }

    private Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        Console.WriteLine(exception.Message);
        return Task.CompletedTask;
    }
}
