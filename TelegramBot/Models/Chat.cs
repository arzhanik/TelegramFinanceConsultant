using System.ComponentModel.DataAnnotations;

namespace TelegramBot.Models;

public class Chat
{
    public int Id { get; set; }

    public long TelegramChatId { get; set; }

    [MaxLength(32)]
    public string ReportToken { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public ICollection<Spending> Spendings { get; set; } = new List<Spending>();
}
