using AttendanceUI.Data;
using AttendanceUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceUI.Pages.Holidays;

public sealed class IndexModel : PageModel
{
    private readonly BiometricAttendanceDbContext _db;

    public IndexModel(BiometricAttendanceDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<Holiday> Holidays { get; private set; } = Array.Empty<Holiday>();

    public async Task OnGetAsync()
    {
        Holidays = await _db.Holidays
            .AsNoTracking()
            .OrderByDescending(h => h.StartDate)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var holiday = await _db.Holidays.Include(h => h.EligibleEmployees).FirstOrDefaultAsync(h => h.Id == id);
        if (holiday is null)
        {
            return NotFound();
        }

        if (holiday.EligibleEmployees != null)
        {
            _db.HolidayEmployees.RemoveRange(holiday.EligibleEmployees);
        }

        _db.Holidays.Remove(holiday);
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }
}
