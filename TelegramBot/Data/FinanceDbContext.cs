using Microsoft.EntityFrameworkCore;
using TelegramBot.Models;

namespace TelegramBot.Data;

public class FinanceDbContext : DbContext
{
    public FinanceDbContext(DbContextOptions<FinanceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Chat> Chats => Set<Chat>();

    public DbSet<Spending> Spendings => Set<Spending>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chat>()
            .HasIndex(x => x.TelegramChatId)
            .IsUnique();

        modelBuilder.Entity<Chat>()
            .HasIndex(x => x.ReportToken)
            .IsUnique();

        modelBuilder.Entity<Spending>()
            .Property(x => x.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Spending>()
            .HasOne(x => x.Chat)
            .WithMany(x => x.Spendings)
            .HasForeignKey(x => x.ChatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
