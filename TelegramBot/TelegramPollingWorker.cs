using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using TelegramBot.Parsing;
using TelegramBot.Services;

namespace TelegramBot;

public class TelegramPollingWorker : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceScopeFactory _scopeFactory;

    public TelegramPollingWorker(ITelegramBotClient botClient, IServiceScopeFactory scopeFactory)
    {
        _botClient = botClient;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        if (update.Message?.Text == null)
            return;

        var chatId = update.Message.Chat.Id;
        var text = update.Message.Text.Trim();

        using var scope = _scopeFactory.CreateScope();
        var finance = scope.ServiceProvider.GetRequiredService<FinanceService>();

        if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            await finance.GetOrCreateChatAsync(chatId, cancellationToken);
            await botClient.SendMessage(chatId, "Hello! Send spending in format: 1000 food lunch", cancellationToken: cancellationToken);
            return;
        }

        if (text.Equals("/today", StringComparison.OrdinalIgnoreCase))
        {
            if (!await finance.HasChatAsync(chatId, cancellationToken))
            {
                await botClient.SendMessage(chatId, "Send /start first.", cancellationToken: cancellationToken);
                return;
            }

            var total = await finance.GetTodayTotalAsync(chatId, cancellationToken);
            await botClient.SendMessage(chatId, $"Today's total: {total:0.##}", cancellationToken: cancellationToken);
            return;
        }

        if (text.Equals("/month", StringComparison.OrdinalIgnoreCase))
        {
            if (!await finance.HasChatAsync(chatId, cancellationToken))
            {
                await botClient.SendMessage(chatId, "Send /start first.", cancellationToken: cancellationToken);
                return;
            }

            var summary = await finance.GetMonthSummaryAsync(chatId, cancellationToken);
            await botClient.SendMessage(
                chatId,
                $"Month total: {summary.Total:0.##}\nTransactions: {summary.Count}",
                cancellationToken: cancellationToken);
            return;
        }

        var parsed = SpendingParser.Parse(text);

        if (parsed == null)
        {
            await botClient.SendMessage(
                chatId,
                "Format: <amount> <category> [note...]\nExample: 1500 food lunch",
                cancellationToken: cancellationToken);
            return;
        }

        if (!await finance.HasChatAsync(chatId, cancellationToken))
        {
            await botClient.SendMessage(chatId, "Send /start first.", cancellationToken: cancellationToken);
            return;
        }

        await finance.AddSpendingAsync(chatId, parsed, cancellationToken);

        await botClient.SendMessage(
            chatId,
            $"Saved: {parsed.Amount:0.##} {parsed.Category}",
            cancellationToken: cancellationToken);
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