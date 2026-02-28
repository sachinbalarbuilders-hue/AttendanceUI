using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AttendanceUI.Data;
using AttendanceUI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AttendanceUI.Controllers;

[ApiController]
[Route("dev")]
public sealed class DevController : ControllerBase
{
    private readonly BiometricAttendanceDbContext _db;
    private readonly IConfiguration _config;

    public DevController(BiometricAttendanceDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // GET /dev/token?sub=hr
    // Development-only: mint a JWT signed with app's Jwt:Key for testing.
    [HttpGet("token")]
    public IActionResult Token([FromQuery] string sub = "dev-user")
    {
        var key = _config["Jwt:Key"] ?? "dev-secret-key-please-change";
        var issuer = _config["Jwt:Issuer"] ?? "AttendanceUI";
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, sub), new Claim("role", "hr") };
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer: issuer, claims: claims, expires: DateTime.UtcNow.AddHours(1), signingCredentials: creds);
        var s = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(s);
    }

    // POST /dev/seed?name=Test%20Employee
    // Create a minimal employee record for testing and return its id.
    [HttpPost("seed")]
    public async Task<IActionResult> Seed([FromQuery] string name = "Test Employee")
    {
        var maxId = await _db.Employees.OrderByDescending(e => e.EmployeeId).Select(e => (int?)e.EmployeeId).FirstOrDefaultAsync() ?? 0;
        var id = maxId + 1;
        var employee = new Employee
        {
            EmployeeId = id,
            EmployeeName = name,
            Status = "active",
            DeviceSynced = 0
        };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();
        return Ok(new { employeeId = id });
    }
}
