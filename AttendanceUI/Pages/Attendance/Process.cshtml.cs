using AttendanceUI.Data;
using AttendanceUI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceUI.Pages.Attendance;

public class ProcessModel : PageModel
{
    private readonly AttendanceProcessorService _processor;
    private readonly ILogger<ProcessModel> _logger;
    private readonly BiometricAttendanceDbContext _db;

    public ProcessModel(AttendanceProcessorService processor, ILogger<ProcessModel> logger, BiometricAttendanceDbContext db)
    {
        _processor = processor;
        _logger = logger;
        _db = db;
    }

    [BindProperty]
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    [BindProperty]
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    [BindProperty]
    public bool ClearFutureData { get; set; } = false;

    [TempData]
    public string Message { get; set; } = "";

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (FromDate > ToDate)
        {
            Message = "Error: From Date cannot be later than To Date.";
            return Page();
        }

        try
        {
            // Clear future data if requested
            if (ClearFutureData)
            {
                var futureRecords = await _db.DailyAttendance
                    .Where(a => a.RecordDate > ToDate)
                    .ToListAsync();
                
                if (futureRecords.Any())
                {
                    _db.DailyAttendance.RemoveRange(futureRecords);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation($"Cleared {futureRecords.Count} attendance records after {ToDate}");
                }
            }

            for (var d = FromDate; d <= ToDate; d = d.AddDays(1))
            {
                await _processor.ProcessDailyAttendanceAsync(d);
            }
            
            var clearMsg = ClearFutureData ? " (Future data cleared)" : "";
            Message = $"Success: Attendance processed from {FromDate} to {ToDate}.{clearMsg}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing attendance.");
            var msg = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                msg += " | Inner: " + inner.Message;
                inner = inner.InnerException;
            }
            Message = $"Error: {msg}";
        }

        return Page();
    }
}
