using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using TelegramBot.Models;
using TelegramBot.Services;

namespace TelegramBot.Controllers;

[ApiController]
[Route("telegram/webhook")]
public class TelegramWebhookController : ControllerBase
{
    private readonly TelegramUpdateHandler _updateHandler;
    private readonly TelegramOptions _options;

    public TelegramWebhookController(
        TelegramUpdateHandler updateHandler,
        IOptions<TelegramOptions> options)
    {
        _updateHandler = updateHandler;
        _options = options.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Post(
        [FromBody] Update update,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(
                _options.Mode,
                "Webhook",
                StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        var secret =
            Request.Headers["X-Telegram-Bot-Api-Secret-Token"].FirstOrDefault();

        if (!string.Equals(
                secret,
                _options.WebhookSecret,
                StringComparison.Ordinal))
        {
            return Unauthorized();
        }

        await _updateHandler.HandleAsync(
            update,
            cancellationToken);

        return Ok();
    }
}
