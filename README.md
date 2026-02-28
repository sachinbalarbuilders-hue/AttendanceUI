# Biometric Attendance & HRMS System

A comprehensive Human Resource Management System (HRMS) integrated with biometric hardware for automated attendance tracking, payroll processing, and leave management.

## 🏗️ Project Architecture

The system consists of two primary components:

### 1. **AttendanceUI** (Web Application)
*Built with: ASP.NET Core Razor Pages, Entity Framework Core, MySQL*
The central management console for HR and Admins.
-   **Employee Management**: Detailed profiles, departments, designations, and shifts.
-   **Attendance & Regularization**: Automated tracking with manual regularization workflows and "sandwich rule" logic.
-   **Leave Management**: Application tracking, Comp-Off expiry logic (90 days), and holiday calendars.
-   **Payroll & Loans**: Monthly payroll processing with installment-based loan deductions.
-   **Reports**: Comprehensive reports for attendance logs, application tracking, and late/early marks.
-   **Device Configurations**: Multi-device management with real-time connectivity testing.

### 2. **Z903AttendanceService** (Windows Service)
*Built with: C# .NET Framework, Biometric SDK (SBXPC)*
A robust background service that bridges the hardware and software.
-   **Auto-Sync**: Periodically pulls punch records from all configured biometric machines.
-   **Incremental Logic**: Uses `device_sync_state` to only fetch new records, optimizing performance.
-   **IPC Interface**: Communicates with the UI via **Named Pipes** for real-time operations like adding users or testing connections.
-   **Robustness**: Features automatic retry logic and exponential backoff for network stability.

---

## 🚀 Getting Started

### Database Setup
1.  Target Database: `biometric_attendance` (MySQL).
2.  Ensure connection strings are updated in:
    -   `AttendanceUI/appsettings.json`
    -   `Z903AttendanceService/App.config`

### Running the UI
1.  Open `/AttendanceUI` in your IDE.
2.  Run `dotnet run`.
3.  Access at `http://localhost:5000`.

### Building/Installing the Service
1.  Open `/source/repos/Z903AttendanceService` in Visual Studio.
2.  Build the solution (`bin/Debug/Z903AttendanceService.exe`).
3.  Install as a Windows Service using `InstallUtil.exe` or a custom installer.

---

## 🛠️ Key Workflows

### Configuring Devices
1.  Navigate to **Masters > Device Settings**.
2.  Add your biometric machines (IP, Port, Machine #).
3.  The Service will automatically begin syncing from these machines in its next cycle.

### Payroll Processing
1.  Ensure all attendance is synced and regularizations are approved.
2.  Navigate to **Payroll > Process Payroll**.
3.  The system calculates salaries, takes into account approved leaves, and automatically deducts loan installments.

### Leave Applications
1.  Employees apply via the UI.
2.  Admins approve/reject. Approved leaves are automatically pulled into attendance calculations.

---

## 📄 Documentation & Support
-   **Maintenance**: Regularly check `service.log` for device connection status.
-   **SDK**: The system uses the SBXPC SDK wrapper for broad compatibility with ZK-style machines.

---

## 📤 Git Instructions
```bash
git add .
git commit -m "Comprehensive HRMS update with multi-device support"
git push origin main
```
