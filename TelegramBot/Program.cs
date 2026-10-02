using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using TelegramBot;
using TelegramBot.Data;
using TelegramBot.Models;
using TelegramBot.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TelegramOptions>(
    builder.Configuration.GetSection("Telegram"));

var botToken =
    builder.Configuration["Telegram:BotToken"]
    ?? throw new InvalidOperationException(
        "Telegram BotToken is not configured.");

builder.Services.AddSingleton<ITelegramBotClient>(
    new TelegramBotClient(botToken));

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=finance.db";

builder.Services.AddDbContext<FinanceDbContext>(
    options => options.UseSqlite(connectionString));

builder.Services.AddScoped<FinanceService>();

builder.Services.AddSingleton<TelegramUpdateHandler>();

builder.Services.AddHostedService<TelegramPollingWorker>();
builder.Services.AddHostedService<TelegramWebhookWorker>();
builder.Services.AddHostedService<DailyDigestService>();

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<FinanceDbContext>();

    await db.Database.EnsureCreatedAsync();
}

app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();
