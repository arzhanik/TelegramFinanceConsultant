using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using TelegramBot.Data;
using TelegramBot.Models;
using TelegramBot.Parsing;

namespace TelegramBot.Services;

public class FinanceService
{
    private readonly FinanceDbContext _db;

    public FinanceService(FinanceDbContext db) => _db = db;

    public async Task<Chat> GetOrCreateChatAsync(long telegramChatId, CancellationToken ct)
    {
        var chat = await _db.Chats.SingleOrDefaultAsync(x => x.TelegramChatId == telegramChatId, ct);
        if (chat != null) return chat;

        chat = new Chat
        {
            TelegramChatId = telegramChatId,
            ReportToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
            StartedAt = DateTime.UtcNow
        };

        _db.Chats.Add(chat);
        await _db.SaveChangesAsync(ct);
        return chat;
    }

    public Task<bool> HasChatAsync(long telegramChatId, CancellationToken ct) =>
        _db.Chats.AnyAsync(x => x.TelegramChatId == telegramChatId, ct);

    public async Task<Spending> AddSpendingAsync(long telegramChatId, ParsedSpending parsed, CancellationToken ct)
    {
        var chat = await GetOrCreateChatAsync(telegramChatId, ct);
        var spending = new Spending
        {
            ChatId = chat.Id,
            Amount = parsed.Amount,
            Category = parsed.Category.ToLowerInvariant(),
            Note = parsed.Note,
            SpentAt = DateTime.UtcNow
        };

        _db.Spendings.Add(spending);
        await _db.SaveChangesAsync(ct);
        return spending;
    }

    public async Task<decimal> GetTodayTotalAsync(long telegramChatId, CancellationToken ct)
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(1);

        return await _db.Spendings
            .Where(x => x.Chat.TelegramChatId == telegramChatId &&
                        x.SpentAt >= start && x.SpentAt < end)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
    }

    public async Task<(decimal Total, int Count)> GetMonthSummaryAsync(long telegramChatId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var query = _db.Spendings.Where(x =>
            x.Chat.TelegramChatId == telegramChatId &&
            x.SpentAt >= start && x.SpentAt < end);

        var total = await query.SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        var count = await query.CountAsync(ct);

        return (total, count);
    }
}