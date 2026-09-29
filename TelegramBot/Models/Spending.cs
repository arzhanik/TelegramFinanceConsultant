using System.ComponentModel.DataAnnotations;

namespace TelegramBot.Models;

public class Spending
{
    public int Id { get; set; }

    public int ChatId { get; set; }

    public Chat Chat { get; set; } = null!;

    public decimal Amount { get; set; }

    [MaxLength(100)]
    public string Category { get; set; } = null!;

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTime SpentAt { get; set; }
}
