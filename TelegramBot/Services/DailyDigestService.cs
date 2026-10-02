using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using TelegramBot.Data;

namespace TelegramBot.Services;

public class DailyDigestService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITelegramBotClient _botClient;
    private readonly IConfiguration _configuration;

    public DailyDigestService(IServiceScopeFactory scopeFactory, ITelegramBotClient botClient, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _botClient = botClient;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var hour = _configuration.GetValue<int>("Telegram:DigestHourUtc", 18);
            var next = new DateTime(now.Year, now.Month, now.Day, hour, 0, 0, DateTimeKind.Utc);

            if (next <= now)
                next = next.AddDays(1);

            await Task.Delay(next - now, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
                await SendDigestsAsync(stoppingToken);
        }
    }

    private async Task SendDigestsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonth = monthStart.AddMonths(1);

        var chats = await db.Chats
            .Include(x => x.Spendings)
            .Where(x => x.Spendings.Any(s => s.SpentAt >= monthStart && s.SpentAt < nextMonth))
            .ToListAsync(ct);

        foreach (var chat in chats)
        {
            var month = chat.Spendings
                .Where(x => x.SpentAt >= monthStart && x.SpentAt < nextMonth)
                .ToList();

            var last7Start = now.Date.AddDays(-6);
            var last7End = now.Date.AddDays(1);
            var previous7Start = now.Date.AddDays(-13);
            var previous7End = now.Date.AddDays(-6);

            var total = month.Sum(x => x.Amount);
            var last7 = month.Where(x => x.SpentAt >= last7Start && x.SpentAt < last7End).Sum(x => x.Amount);
            var previous7 = month.Where(x => x.SpentAt >= previous7Start && x.SpentAt < previous7End).Sum(x => x.Amount);
            var typicalDay = total / now.Day;

            var top = month
                .GroupBy(x => x.Category)
                .Select(g => new { Category = g.Key, Total = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Total)
                .Take(3);

            var baseUrl = _configuration["PublicBaseUrl"]?.TrimEnd('/');
            var reportUrl = $"{baseUrl}/report/{chat.ReportToken}";

            var lines = new List<string>
            {
                $"Month total: {total:0.##}",
                $"Transactions: {month.Count}",
                $"Last 7 days: {last7:0.##}",
                $"Previous 7 days: {previous7:0.##}",
                $"Typical day: {typicalDay:0.##}",
                "",
                "Top 3 categories:"
            };

            lines.AddRange(top.Select(x => $"{x.Category}: {x.Total:0.##}"));
            lines.Add("");
            lines.Add($"Report: {reportUrl}");

            await _botClient.SendMessage(
                chatId: chat.TelegramChatId,
                text: string.Join('\n', lines),
                cancellationToken: ct);
        }
    }
}