using Telegram.Bot.Types;
using TelegramBot.Data;

namespace TelegramBot.Services;

public class TelegramUpdateHandler
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TelegramUpdateHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task HandleAsync(
        Update update,
        CancellationToken cancellationToken)
    {
        if (update.Message?.Text is null)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();

        var financeService =
            scope.ServiceProvider.GetRequiredService<FinanceService>();

        await financeService.HandleMessageAsync(
            update.Message.Chat.Id,
            update.Message.Text.Trim(),
            cancellationToken);
    }
}
