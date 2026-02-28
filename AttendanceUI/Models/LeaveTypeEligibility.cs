using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendanceUI.Models;

[Table("leave_type_eligibility")]
public class LeaveTypeEligibility
{
    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Column("leave_type_id")]
    public int LeaveTypeId { get; set; }

    public Employee? Employee { get; set; }
    public LeaveType? LeaveType { get; set; }
}
