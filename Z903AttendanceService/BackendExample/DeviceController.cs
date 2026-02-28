using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace BackendExample
{
    [ApiController]
    [Route("api/device")]
    public class DeviceController : ControllerBase
    {
        private readonly MyDbContext _db;
        private readonly IConfiguration _config;

        public DeviceController(MyDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost("set-user/{employeeId}")]
        public async Task<IActionResult> SetUser(int employeeId)
        {
            var emp = await _db.Employees.FindAsync(employeeId);
            if (emp == null) return NotFound("Employee not found");

            // Call local named pipe server
            var resp = Z903AttendanceService.NamedPipeClient.SendSetUser(emp.EmployeeId, emp.EmployeeName, Z903AttendanceService.PipeConstants.PipeName);

            if (resp.Success)
            {
                emp.DeviceSynced = true;
                emp.DeviceSyncError = null;
                await _db.SaveChangesAsync();
                return Ok("Employee successfully registered in machine");
            }
            else
            {
                emp.DeviceSyncError = resp.Message;
                await _db.SaveChangesAsync();
                return BadRequest(resp.Message);
            }
        }
    }
}
