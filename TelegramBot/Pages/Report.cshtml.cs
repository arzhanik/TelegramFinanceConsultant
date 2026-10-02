using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TelegramBot.Services;

namespace TelegramBot.Pages;

public class ReportModel : PageModel
{
    private readonly FinanceService _financeService;

    public FinanceReport? Report { get; private set; }

    public ReportModel(FinanceService financeService)
    {
        _financeService = financeService;
    }

    public async Task<IActionResult> OnGetAsync(
        string token,
        CancellationToken cancellationToken)
    {
        Report = await _financeService.GetReportAsync(
            token,
            cancellationToken);

        if (Report is null)
            return NotFound();

        return Page();
    }
}
