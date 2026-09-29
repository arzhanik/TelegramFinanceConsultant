using System.Globalization;
using System.Text.RegularExpressions;

namespace TelegramBot.Parsing;

public static class SpendingParser
{
    private static readonly Regex CategoryRegex =
        new("^[a-zA-Z]+$", RegexOptions.Compiled);

    public static ParsedSpending? Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var parts = text.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
            return null;

        var amountText = parts[0].Replace(',', '.');

        if (!decimal.TryParse(
                amountText,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var amount))
        {
            return null;
        }

        if (amount <= 0)
            return null;

        var category = parts[1].ToLowerInvariant();

        if (!CategoryRegex.IsMatch(category))
            return null;

        string? note = parts.Length > 2
            ? string.Join(' ', parts.Skip(2))
            : null;

        return new ParsedSpending
        {
            Amount = amount,
            Category = category,
            Note = note
        };
    }
}
