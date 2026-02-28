using System;
using System.Threading.Tasks;

namespace AttendanceUI.Services;

public sealed class DatabaseService
{
    // Synchronous wrapper that calls the Windows service client and throws on failure.
    public void SetUserInMachine(int employeeId, string employeeName)
    {
        var task = WindowsServiceClient.SetNameInMachineAsync(employeeId, employeeName);
        var result = task.GetAwaiter().GetResult();
        if (!result.Success)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "Unknown error from Windows service");
        }
    }
}
