namespace AttendanceUI.Models;

public sealed class HolidayEmployee
{
    public int HolidayId { get; set; }
    public int EmployeeId { get; set; }

    public Holiday? Holiday { get; set; }
    public Employee? Employee { get; set; }
}
