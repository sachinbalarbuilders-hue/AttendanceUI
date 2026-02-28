using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AttendanceUI.Data;
using AttendanceUI.Models;

namespace AttendanceUI.Pages;

public class IndexModel : PageModel
{
    private readonly BiometricAttendanceDbContext _context;

    public IndexModel(BiometricAttendanceDbContext context)
    {
        _context = context;
    }

    public int TotalEmployees { get; set; }
    public int PresentToday { get; set; }
    public int PendingLeaves { get; set; }
    public int PendingRegularizations { get; set; }

    public async Task OnGetAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        // 1. Total Active Employees
        TotalEmployees = await _context.Employees.CountAsync(e => e.Status == "active");

        // 2. Present Today (Employees with attendance records for today)
        // Note: Using ANY attendance record. You might want to filter by InTime != null if strict check needed.
        PresentToday = await _context.DailyAttendance
            .Where(da => da.RecordDate == today && da.InTime != null)
            .CountAsync();

        // 3. Pending Leave Applications
        if (_context.LeaveApplications != null)
        {
            PendingLeaves = await _context.LeaveApplications
                .CountAsync(l => l.Status == "Pending");
        }

        // 4. Pending Regularizations
        if (_context.AttendanceRegularizations != null)
        {
            PendingRegularizations = await _context.AttendanceRegularizations
                .CountAsync(r => r.Status == "Pending");
        }
    }
}
