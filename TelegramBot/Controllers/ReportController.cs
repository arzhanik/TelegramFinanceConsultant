using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelegramBot.Services;

namespace TelegramBot.Controllers;

[ApiController]
[AllowAnonymous]
[Route("report")]
public class ReportController : ControllerBase
{
    private readonly FinanceService _financeService;

    public ReportController(FinanceService financeService)
    {
        _financeService = financeService;
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> Get(
        string token,
        CancellationToken cancellationToken)
    {
        var report = await _financeService.GetReportAsync(
            token,
            cancellationToken);

        if (report is null)
            return NotFound();

        return Ok(report);
    }
}
