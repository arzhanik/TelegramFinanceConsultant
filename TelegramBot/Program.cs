using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using TelegramBot.Data;
using TelegramBot.Services;

namespace TelegramBot;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        builder.Services.AddDbContext<FinanceDbContext>(options =>
            options.UseSqlite("Data Source=finance.db"));

        var botToken = builder.Configuration["Telegram:BotToken"]
                       ?? throw new InvalidOperationException("Telegram BotToken is not configured.");

        builder.Services.AddSingleton<ITelegramBotClient>(
            new TelegramBotClient(botToken));

        builder.Services.AddScoped<FinanceService>();

        builder.Services.AddHostedService<TelegramPollingWorker>();
        builder.Services.AddHostedService<DailyDigestService>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
            db.Database.EnsureCreated();
        }

        app.MapControllers();
        app.Run();
    }
}