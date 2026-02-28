namespace AttendanceUI.Models;

public class ManualAdjustment
{
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
    public string Type { get; set; } = "Allowance"; // Allowance or Deduction
}
