namespace TelegramBot.Parsing;

public class ParsedSpending
{
    public decimal Amount { get; init; }

    public string Category { get; init; } = null!;

    public string? Note { get; init; }
}
