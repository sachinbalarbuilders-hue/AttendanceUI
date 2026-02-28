using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AttendanceUI.Data;
using AttendanceUI.Models;

namespace AttendanceUI.Pages.Payroll.EmployeeSalary
{
    public class EditModel : PageModel
    {
        private readonly BiometricAttendanceDbContext _context;

        public EditModel(BiometricAttendanceDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int EmployeeId { get; set; }

        [BindProperty]
        public string EmployeeName { get; set; } = "";

        [BindProperty]
        public decimal BasicSalary { get; set; }

        [BindProperty]
        public decimal SpecialAllowance { get; set; }

        [BindProperty]
        public decimal ProvidentFund { get; set; }

        [BindProperty]
        public decimal ESIC { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound();

            EmployeeId = id;
            EmployeeName = employee.EmployeeName;

            var salaryComponents = await _context.SalaryComponents.ToListAsync();
            var basicComponent = salaryComponents.FirstOrDefault(c => c.ComponentCode == "BASIC");
            var specialComponent = salaryComponents.FirstOrDefault(c => c.ComponentCode == "SPECIAL");

            if (basicComponent != null)
            {
                var basic = await _context.EmployeeSalaryStructures
                    .FirstOrDefaultAsync(s => s.EmployeeId == id && s.ComponentId == basicComponent.Id && s.IsActive);
                BasicSalary = basic?.Amount ?? 0;
            }

            if (specialComponent != null)
            {
                var special = await _context.EmployeeSalaryStructures
                    .FirstOrDefaultAsync(s => s.EmployeeId == id && s.ComponentId == specialComponent.Id && s.IsActive);
                SpecialAllowance = special?.Amount ?? 0;
            }

            var pfComponent = salaryComponents.FirstOrDefault(c => c.ComponentCode == "PF");
            if (pfComponent != null)
            {
                var pf = await _context.EmployeeSalaryStructures
                    .FirstOrDefaultAsync(s => s.EmployeeId == id && s.ComponentId == pfComponent.Id && s.IsActive);
                ProvidentFund = pf?.Amount ?? 0;
            }

            var esicComponent = salaryComponents.FirstOrDefault(c => c.ComponentCode == "ESIC");
            if (esicComponent != null)
            {
                var esic = await _context.EmployeeSalaryStructures
                    .FirstOrDefaultAsync(s => s.EmployeeId == id && s.ComponentId == esicComponent.Id && s.IsActive);
                ESIC = esic?.Amount ?? 0;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var salaryComponents = await _context.SalaryComponents.ToListAsync();
            var basicComponent = salaryComponents.FirstOrDefault(c => c.ComponentCode == "BASIC");
            var specialComponent = salaryComponents.FirstOrDefault(c => c.ComponentCode == "SPECIAL");

            var effectiveDate = DateOnly.FromDateTime(DateTime.Now);

            // Update or create Basic Salary
            if (basicComponent != null)
            {
                var existing = await _context.EmployeeSalaryStructures
                    .FirstOrDefaultAsync(s => s.EmployeeId == EmployeeId && s.ComponentId == basicComponent.Id && s.IsActive);

                if (existing != null)
                {
                    existing.Amount = BasicSalary;
                }
                else
                {
                    _context.EmployeeSalaryStructures.Add(new EmployeeSalaryStructure
                    {
                        EmployeeId = EmployeeId,
                        ComponentId = basicComponent.Id,
                        Amount = BasicSalary,
                        EffectiveFrom = effectiveDate,
                        IsActive = true
                    });
                }
            }

            // Update or create Special Allowance
            if (specialComponent != null)
            {
                var existing = await _context.EmployeeSalaryStructures
                    .FirstOrDefaultAsync(s => s.EmployeeId == EmployeeId && s.ComponentId == specialComponent.Id && s.IsActive);

                if (existing != null)
                {
                    existing.Amount = SpecialAllowance;
                }
                else
                {
                    _context.EmployeeSalaryStructures.Add(new EmployeeSalaryStructure
                    {
                        EmployeeId = EmployeeId,
                        ComponentId = specialComponent.Id,
                        Amount = SpecialAllowance,
                        EffectiveFrom = effectiveDate,
                        IsActive = true
                    });
                }
            }

            // Update or create PF
            var pfComponent = salaryComponents.FirstOrDefault(c => c.ComponentCode == "PF");
            if (pfComponent != null)
            {
                var existing = await _context.EmployeeSalaryStructures
                    .FirstOrDefaultAsync(s => s.EmployeeId == EmployeeId && s.ComponentId == pfComponent.Id && s.IsActive);

                if (existing != null)
                {
                    existing.Amount = ProvidentFund;
                }
                else
                {
                    _context.EmployeeSalaryStructures.Add(new EmployeeSalaryStructure
                    {
                        EmployeeId = EmployeeId,
                        ComponentId = pfComponent.Id,
                        Amount = ProvidentFund,
                        EffectiveFrom = effectiveDate,
                        IsActive = true
                    });
                }
            }

            // Update or create ESIC
            var esicComponent = salaryComponents.FirstOrDefault(c => c.ComponentCode == "ESIC");
            if (esicComponent != null)
            {
                var existing = await _context.EmployeeSalaryStructures
                    .FirstOrDefaultAsync(s => s.EmployeeId == EmployeeId && s.ComponentId == esicComponent.Id && s.IsActive);

                if (existing != null)
                {
                    existing.Amount = ESIC;
                }
                else
                {
                    _context.EmployeeSalaryStructures.Add(new EmployeeSalaryStructure
                    {
                        EmployeeId = EmployeeId,
                        ComponentId = esicComponent.Id,
                        Amount = ESIC,
                        EffectiveFrom = effectiveDate,
                        IsActive = true
                    });
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
