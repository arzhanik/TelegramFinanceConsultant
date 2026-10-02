using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.Data;
using TelegramBot.Models;
using TelegramBot.Parsing;

namespace TelegramBot.Services;

public class FinanceService
{
    private readonly FinanceDbContext _db;
    private readonly ITelegramBotClient _bot;
    private readonly TelegramOptions _options;

    public FinanceService(
        FinanceDbContext db,
        ITelegramBotClient bot,
        IOptions<TelegramOptions> options)
    {
        _db = db;
        _bot = bot;
        _options = options.Value;
    }

    public async Task HandleMessageAsync(
        long telegramChatId,
        string text,
        CancellationToken cancellationToken)
    {
        if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            await StartAsync(telegramChatId, cancellationToken);
            return;
        }

        if (text.Equals("/today", StringComparison.OrdinalIgnoreCase))
        {
            await SendTodayAsync(telegramChatId, cancellationToken);
            return;
        }

        if (text.Equals("/month", StringComparison.OrdinalIgnoreCase))
        {
            await SendMonthAsync(telegramChatId, cancellationToken);
            return;
        }

        var parsed = SpendingParser.Parse(text);

        if (parsed is null)
        {
            await _bot.SendMessage(
                telegramChatId,
                "Invalid format. Use: <amount> <category> [note...]",
                cancellationToken: cancellationToken);

            return;
        }

        await AddSpendingAsync(
            telegramChatId,
            parsed,
            cancellationToken);
    }

    private async Task StartAsync(
        long telegramChatId,
        CancellationToken cancellationToken)
    {
        var chat = await _db.Chats
            .SingleOrDefaultAsync(
                x => x.TelegramChatId == telegramChatId,
                cancellationToken);

        if (chat is null)
        {
            chat = new Chat
            {
                TelegramChatId = telegramChatId,
                ReportToken = Convert.ToHexString(
                    RandomNumberGenerator.GetBytes(16)),
                StartedAt = DateTime.UtcNow
            };

            _db.Chats.Add(chat);
        }
        else
        {
            chat.StartedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _bot.SendMessage(
            telegramChatId,
            "Welcome! Send spending like:\n1500 food lunch\n500 transport taxi\n\nCommands:\n/today\n/month",
            cancellationToken: cancellationToken);
    }

    private async Task AddSpendingAsync(
        long telegramChatId,
        ParsedSpending parsed,
        CancellationToken cancellationToken)
    {
        var chat = await _db.Chats
            .SingleOrDefaultAsync(
                x => x.TelegramChatId == telegramChatId,
                cancellationToken);

        if (chat is null)
        {
            await _bot.SendMessage(
                telegramChatId,
                "Please send /start first.",
                cancellationToken: cancellationToken);

            return;
        }

        _db.Spendings.Add(new Spending
        {
            ChatId = chat.Id,
            Amount = parsed.Amount,
            Category = parsed.Category,
            Note = parsed.Note,
            SpentAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        await _bot.SendMessage(
            telegramChatId,
            $"Saved: {parsed.Amount:0.##} {_options.Currency} — {parsed.Category}",
            cancellationToken: cancellationToken);
    }

    private async Task SendTodayAsync(
        long telegramChatId,
        CancellationToken cancellationToken)
    {
        var chat = await _db.Chats
            .SingleOrDefaultAsync(
                x => x.TelegramChatId == telegramChatId,
                cancellationToken);

        if (chat is null)
        {
            await _bot.SendMessage(
                telegramChatId,
                "Please send /start first.",
                cancellationToken: cancellationToken);

            return;
        }

        var now = DateTime.UtcNow;
        var start = now.Date;
        var end = start.AddDays(1);

        var spendings = await _db.Spendings
            .Where(x =>
                x.ChatId == chat.Id &&
                x.SpentAt >= start &&
                x.SpentAt < end)
            .ToListAsync(cancellationToken);

        var total = spendings.Sum(x => x.Amount);

        await _bot.SendMessage(
            telegramChatId,
            $"Today\nTotal: {total:0.##} {_options.Currency}\nCount: {spendings.Count}",
            cancellationToken: cancellationToken);
    }

    private async Task SendMonthAsync(
        long telegramChatId,
        CancellationToken cancellationToken)
    {
        var chat = await _db.Chats
            .SingleOrDefaultAsync(
                x => x.TelegramChatId == telegramChatId,
                cancellationToken);

        if (chat is null)
        {
            await _bot.SendMessage(
                telegramChatId,
                "Please send /start first.",
                cancellationToken: cancellationToken);

            return;
        }

        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1);
        var end = start.AddMonths(1);

        var spendings = await _db.Spendings
            .Where(x =>
                x.ChatId == chat.Id &&
                x.SpentAt >= start &&
                x.SpentAt < end)
            .ToListAsync(cancellationToken);

        var total = spendings.Sum(x => x.Amount);

        await _bot.SendMessage(
            telegramChatId,
            $"This month\nTotal: {total:0.##} {_options.Currency}\nCount: {spendings.Count}",
            cancellationToken: cancellationToken);
    }

    public async Task SendDigestAsync(
        long telegramChatId,
        CancellationToken cancellationToken)
    {
        var chat = await _db.Chats
            .SingleOrDefaultAsync(
                x => x.TelegramChatId == telegramChatId,
                cancellationToken);

        if (chat is null)
            return;

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var monthSpendings = await _db.Spendings
            .Where(x =>
                x.ChatId == chat.Id &&
                x.SpentAt >= monthStart &&
                x.SpentAt < monthEnd)
            .ToListAsync(cancellationToken);

        if (monthSpendings.Count == 0)
            return;

        var last7Start = now.AddDays(-7);
        var previous7Start = now.AddDays(-14);

        var last7Total = monthSpendings
            .Where(x => x.SpentAt >= last7Start && x.SpentAt <= now)
            .Sum(x => x.Amount);

        var previous7Total = monthSpendings
            .Where(x =>
                x.SpentAt >= previous7Start &&
                x.SpentAt < last7Start)
            .Sum(x => x.Amount);

        var daysElapsed = Math.Max(1, now.Day);

        var typicalDay = monthSpendings.Sum(x => x.Amount) / daysElapsed;

        var topCategories = monthSpendings
            .GroupBy(x => x.Category)
            .OrderByDescending(x => x.Sum(y => y.Amount))
            .Take(3)
            .Select(x =>
                $"{x.Key}: {x.Sum(y => y.Amount):0.##} {_options.Currency}");

        var reportUrl =
            $"{_options.PublicBaseUrl.TrimEnd('/')}/report/{chat.ReportToken}";

        var message =
            $"Monthly digest\n\n" +
            $"Month total: {monthSpendings.Sum(x => x.Amount):0.##} {_options.Currency}\n" +
            $"Count: {monthSpendings.Count}\n" +
            $"Last 7 days: {last7Total:0.##} {_options.Currency}\n" +
            $"Previous 7 days: {previous7Total:0.##} {_options.Currency}\n" +
            $"Typical day: {typicalDay:0.##} {_options.Currency}\n\n" +
            $"Top categories:\n{string.Join("\n", topCategories)}\n\n" +
            $"Report:\n{reportUrl}";

        await _bot.SendMessage(
            telegramChatId,
            message,
            cancellationToken: cancellationToken);
    }

    public async Task<FinanceReport?> GetReportAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var chat = await _db.Chats
            .SingleOrDefaultAsync(
                x => x.ReportToken == token,
                cancellationToken);

        if (chat is null)
            return null;

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var monthSpendings = await _db.Spendings
            .Where(x =>
                x.ChatId == chat.Id &&
                x.SpentAt >= monthStart &&
                x.SpentAt < monthEnd)
            .OrderByDescending(x => x.SpentAt)
            .ToListAsync(cancellationToken);

        var last7Start = now.AddDays(-7);
        var previous7Start = now.AddDays(-14);

        var last7Total = monthSpendings
            .Where(x => x.SpentAt >= last7Start && x.SpentAt <= now)
            .Sum(x => x.Amount);

        var previous7Total = monthSpendings
            .Where(x =>
                x.SpentAt >= previous7Start &&
                x.SpentAt < last7Start)
            .Sum(x => x.Amount);

        var daysElapsed = Math.Max(1, now.Day);

        var categories = monthSpendings
            .GroupBy(x => x.Category)
            .Select(x => new CategoryReport
            {
                Category = x.Key,
                Total = x.Sum(y => y.Amount),
                Count = x.Count()
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        var daily = Enumerable
            .Range(0, 14)
            .Select(i => now.Date.AddDays(-13 + i))
            .Select(date => new DailyReport
            {
                Date = date,
                Total = monthSpendings
                    .Where(x => x.SpentAt.Date == date)
                    .Sum(x => x.Amount)
            })
            .ToList();

        var recent = monthSpendings
            .Take(20)
            .Select(x => new SpendingReport
            {
                Amount = x.Amount,
                Category = x.Category,
                Note = x.Note,
                SpentAt = x.SpentAt
            })
            .ToList();

        return new FinanceReport
        {
            MonthTotal = monthSpendings.Sum(x => x.Amount),
            MonthCount = monthSpendings.Count,
            Last7Total = last7Total,
            Previous7Total = previous7Total,
            TypicalDay = monthSpendings.Sum(x => x.Amount) / daysElapsed,
            Currency = _options.Currency,
            Categories = categories,
            Daily = daily,
            RecentSpendings = recent
        };
    }
}

public class FinanceReport
{
    public decimal MonthTotal { get; init; }
    public int MonthCount { get; init; }
    public decimal Last7Total { get; init; }
    public decimal Previous7Total { get; init; }
    public decimal TypicalDay { get; init; }
    public string Currency { get; init; } = "AMD";
    public List<CategoryReport> Categories { get; init; } = [];
    public List<DailyReport> Daily { get; init; } = [];
    public List<SpendingReport> RecentSpendings { get; init; } = [];
}

public class CategoryReport
{
    public string Category { get; init; } = "";
    public decimal Total { get; init; }
    public int Count { get; init; }
}

public class DailyReport
{
    public DateTime Date { get; init; }
    public decimal Total { get; init; }
}

public class SpendingReport
{
    public decimal Amount { get; init; }
    public string Category { get; init; } = "";
    public string? Note { get; init; }
    public DateTime SpentAt { get; init; }
}
