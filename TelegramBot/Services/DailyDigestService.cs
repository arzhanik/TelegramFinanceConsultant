using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TelegramBot.Data;
using TelegramBot.Models;

namespace TelegramBot.Services;

public class DailyDigestService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TelegramOptions _options;

    public DailyDigestService(
        IServiceScopeFactory scopeFactory,
        IOptions<TelegramOptions> options)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date
                .AddDays(now.Hour >= _options.DigestHourUtc ? 1 : 0)
                .AddHours(_options.DigestHourUtc);

            var delay = nextRun - now;

            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                break;

            await SendDigestsAsync(stoppingToken);
        }
    }

    private async Task SendDigestsAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<FinanceDbContext>();

        var financeService = scope.ServiceProvider
            .GetRequiredService<FinanceService>();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var chatIds = await db.Spendings
            .Where(x =>
                x.SpentAt >= monthStart &&
                x.SpentAt < monthEnd)
            .Select(x => x.ChatId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var chats = await db.Chats
            .Where(x => chatIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        foreach (var chat in chats)
        {
            await financeService.SendDigestAsync(
                chat.TelegramChatId,
                cancellationToken);
        }
    }
}
