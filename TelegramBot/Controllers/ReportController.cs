using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelegramBot.Data;

namespace TelegramBot.Controllers;

[ApiController]
[AllowAnonymous]
[Route("report")]
public class ReportController : ControllerBase
{
    private readonly FinanceDbContext _db;

    public ReportController(FinanceDbContext db) => _db = db;

    [HttpGet("{token}")]
    public async Task<IActionResult> GetReport(string token, CancellationToken ct)
    {
        var chat = await _db.Chats
            .Include(x => x.Spendings)
            .SingleOrDefaultAsync(x => x.ReportToken == token, ct);

        if (chat == null)
            return NotFound();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonth = monthStart.AddMonths(1);

        var month = chat.Spendings
            .Where(x => x.SpentAt >= monthStart && x.SpentAt < nextMonth)
            .OrderByDescending(x => x.SpentAt)
            .ToList();

        var total = month.Sum(x => x.Amount);
        var last7 = month.Where(x => x.SpentAt >= now.Date.AddDays(-6) && x.SpentAt < now.Date.AddDays(1)).Sum(x => x.Amount);
        var previous7 = month.Where(x => x.SpentAt >= now.Date.AddDays(-13) && x.SpentAt < now.Date.AddDays(-6)).Sum(x => x.Amount);
        var typicalDay = total / now.Day;

        var categories = month
            .GroupBy(x => x.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(x => x.Amount), Count = g.Count() })
            .OrderByDescending(x => x.Total)
            .ToList();

        var categoryRows = string.Join("", categories.Select(x =>
            $"<tr><td>{Encode(x.Category)}</td><td>{x.Total:0.##}</td><td>{x.Count}</td></tr>"));

        var dailyRows = string.Join("", Enumerable.Range(0, 14).Select(i =>
        {
            var date = now.Date.AddDays(-13 + i);
            var dayTotal = chat.Spendings
                .Where(x => x.SpentAt >= date && x.SpentAt < date.AddDays(1))
                .Sum(x => x.Amount);

            return $"<tr><td>{date:yyyy-MM-dd}</td><td>{dayTotal:0.##}</td></tr>";
        }));

        var recentRows = string.Join("", chat.Spendings
            .OrderByDescending(x => x.SpentAt)
            .Take(20)
            .Select(x =>
                $"<tr><td>{x.SpentAt:yyyy-MM-dd HH:mm:ss}</td><td>{x.Amount:0.##}</td><td>{Encode(x.Category)}</td><td>{Encode(x.Note ?? "")}</td></tr>"));

        var html =
            "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Finance Report</title></head><body>" +
            "<h1>Finance Report</h1>" +
            $"<p>Month total: <strong>{total:0.##}</strong></p>" +
            $"<p>Transactions: <strong>{month.Count}</strong></p>" +
            $"<p>Last 7 days: <strong>{last7:0.##}</strong></p>" +
            $"<p>Previous 7 days: <strong>{previous7:0.##}</strong></p>" +
            $"<p>Typical day: <strong>{typicalDay:0.##}</strong></p>" +
            "<h2>Categories</h2><table border=\"1\"><tr><th>Category</th><th>Total</th><th>Count</th></tr>" +
            categoryRows +
            "</table><h2>Last 14 days</h2><table border=\"1\"><tr><th>Date</th><th>Total</th></tr>" +
            dailyRows +
            "</table><h2>Last 20 spendings</h2><table border=\"1\"><tr><th>Date</th><th>Amount</th><th>Category</th><th>Note</th></tr>" +
            recentRows +
            "</table></body></html>";

        return Content(html, "text/html");
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}