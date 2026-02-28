namespace AttendanceUI.Models;

public sealed class Designation
{
    public int Id { get; set; }

    public string DesignationName { get; set; } = "";

    public string? Status { get; set; }
}
