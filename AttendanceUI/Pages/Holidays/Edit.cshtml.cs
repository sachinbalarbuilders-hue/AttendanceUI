using System.ComponentModel.DataAnnotations;
using AttendanceUI.Data;
using AttendanceUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AttendanceUI.Pages.Holidays;

public sealed class EditModel : PageModel
{
    private readonly BiometricAttendanceDbContext _db;

    public EditModel(BiometricAttendanceDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public FormInput Input { get; set; } = new();

    public List<Employee> Employees { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var holiday = await _db.Holidays
            .Include(h => h.EligibleEmployees)
            .FirstOrDefaultAsync(h => h.Id == Id);

        if (holiday is null)
        {
            return NotFound();
        }

        Input = new FormInput
        {
            HolidayName = holiday.HolidayName,
            StartDate = holiday.StartDate,
            EndDate = holiday.EndDate,
            Description = holiday.Description,
            IsGlobal = holiday.IsGlobal,
            EmployeeIds = holiday.EligibleEmployees?.Select(e => e.EmployeeId).ToList()
        };

        Employees = await _db.Employees.Where(e => e.Status == "active").OrderBy(e => e.EmployeeName).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Employees = await _db.Employees.Where(e => e.Status == "active").OrderBy(e => e.EmployeeName).ToListAsync();
            return Page();
        }

        var holiday = await _db.Holidays
            .Include(h => h.EligibleEmployees)
            .FirstOrDefaultAsync(h => h.Id == Id);

        if (holiday is null)
        {
            return NotFound();
        }

        holiday.HolidayName = Input.HolidayName.Trim();
        holiday.StartDate = Input.StartDate!.Value;
        holiday.EndDate = Input.EndDate ?? holiday.StartDate;
        holiday.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();
        holiday.IsGlobal = Input.IsGlobal;

        // Update employee mapping
        if (holiday.EligibleEmployees != null)
        {
            _db.HolidayEmployees.RemoveRange(holiday.EligibleEmployees);
        }

        if (!Input.IsGlobal && Input.EmployeeIds != null && Input.EmployeeIds.Any())
        {
            foreach (var empId in Input.EmployeeIds)
            {
                _db.HolidayEmployees.Add(new HolidayEmployee
                {
                    HolidayId = holiday.Id,
                    EmployeeId = empId
                });
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage("./Index");
    }

    public sealed class FormInput
    {
        [Required]
        [StringLength(255)]
        [Display(Name = "Holiday Name")]
        public string HolidayName { get; set; } = "";

        [Required]
        [Display(Name = "From Date")]
        public DateOnly? StartDate { get; set; }

        [Display(Name = "To Date")]
        public DateOnly? EndDate { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Applicable to All Employees")]
        public bool IsGlobal { get; set; } = true;

        public List<int>? EmployeeIds { get; set; }
    }
}
