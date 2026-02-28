namespace AttendanceUI.Models;

public sealed class Department
{
    public int Id { get; set; }

    public string DepartmentName { get; set; } = "";

    public string? Status { get; set; }
}
