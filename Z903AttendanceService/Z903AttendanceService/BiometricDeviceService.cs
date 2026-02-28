using System;
using System.Configuration;
using System.Reflection;
using System.Threading;

namespace Z903AttendanceService
{
    /// <summary>
    /// Encapsulates biometric SDK interactions. Windows Service should own this class.
    /// </summary>
    public class BiometricDeviceService
    {
        private readonly Action<string> _logger;

        // SDKs are often not thread-safe; protect calls with a lock and initialize once.
        internal static readonly object SdkLock = new object();
        internal static bool SdkInitialized = false;

        private const int DefaultMachineNumber = 1;

        // Dynamic Configuration
        public static class DeviceConfig
        {
            public static string IpAddress { get; private set; }
            public static int Port { get; private set; }
            public static int MachineNumber { get; private set; }
            public static int CommKey { get; private set; } = 0;

            static DeviceConfig()
            {
                // Initialize with defaults from App.config or fallbacks
                IpAddress = ConfigurationManager.AppSettings["DeviceIp"] ?? string.Empty;
                string portStr = ConfigurationManager.AppSettings["DevicePort"];
                Port = int.TryParse(portStr, out int p) ? p : 0;
                MachineNumber = 1; 
            }

            public static void Update(string ip, int port, int machineNumber, int commKey)
            {
                IpAddress = ip;
                Port = port;
                MachineNumber = machineNumber;
                CommKey = commKey;
            }
        }

        public BiometricDeviceService(Action<string> logger = null)
        {
            _logger = logger;
        }

        private void Log(string message)
        {
            _logger?.Invoke($"[BiometricDeviceService] {message}");
        }

        public void SetUserInMachine(int employeeId, string employeeName, string deviceIp = null, int devicePort = 0, int machineNumber = 0, int commKey = 0)
        {
            deviceIp = deviceIp ?? DeviceConfig.IpAddress;
            devicePort = devicePort != 0 ? devicePort : DeviceConfig.Port;
            machineNumber = machineNumber != 0 ? machineNumber : DeviceConfig.MachineNumber;
            commKey = commKey != 0 ? commKey : DeviceConfig.CommKey;

            if (!Monitor.TryEnter(SdkLock, TimeSpan.FromSeconds(30)))
            {
                Log("ERROR: SetUserInMachine timed out waiting for SDK lock. Another operation (likely sync) is holding it.");
                throw new Exception("Timed out waiting for biometric SDK access. Please try again in a few moments.");
            }

            try
            {
                try
                {
                    if (!SdkInitialized)
                    {
                        sbxpc.SBXPCDLL.DotNET();
                        SdkInitialized = true;
                    }

                    // Proactive disconnect to clear any stale sessions
                    try { sbxpc.SBXPCDLL.Disconnect(machineNumber); } catch { }

                    Log($"Connecting to device {deviceIp}:{devicePort} (machineNumber={machineNumber})...");
                    bool connected = sbxpc.SBXPCDLL.ConnectTcpip(machineNumber, deviceIp, devicePort, commKey);
                    if (!connected)
                    {
                        int lastErr = 0;
                        try { int tmp; sbxpc.SBXPCDLL.GetLastError(machineNumber, out tmp); lastErr = tmp; } catch { }
                        Log($"ERROR: ConnectTcpip failed to {deviceIp}:{devicePort} (machineNumber={machineNumber}). SDK GetLastError={lastErr}");
                        throw new Exception($"Failed to connect to biometric device {deviceIp}:{devicePort}. SDK GetLastError={lastErr}");
                    }
                    Log("Connected to biometric device.");

                    Log($"Creating user {employeeId} on device with password (backup 15)...");
                    
                    string passwordStr = employeeId.ToString();
                    int encodedPassword = 0;
                    for (int i = passwordStr.Length - 1; i >= 0; i--)
                    {
                        int digit = int.Parse(passwordStr[i].ToString());
                        encodedPassword = encodedPassword * 16 + (digit + 1);
                    }
                    Log($"Password '{passwordStr}' encoded as: {encodedPassword}");
                    
                    int privilege = 0; // Normal user
                    
                    bool enrollOk = sbxpc.SBXPCDLL.SetEnrollData1(machineNumber, employeeId, 15, privilege, IntPtr.Zero, encodedPassword);
                    if (!enrollOk)
                    {
                        int sdkErr = 0;
                        try { int tmp; sbxpc.SBXPCDLL.GetLastError(machineNumber, out tmp); sdkErr = tmp; } catch { }
                        Log($"Note: SetEnrollData1 returned false for employee {employeeId}. SDK GetLastError={sdkErr}. Will try to set name anyway.");
                    }
                    else
                    {
                        Log($"User {employeeId} created on device with password.");
                    }

                    bool setName = sbxpc.SBXPCDLL.SetUserName1(machineNumber, employeeId, employeeName);
                    if (!setName)
                    {
                        int sdkErr = 0;
                        try { int tmp; sbxpc.SBXPCDLL.GetLastError(machineNumber, out tmp); sdkErr = tmp; } catch { }
                        throw new Exception($"SDK SetUserName1 call failed. SDK error code: {sdkErr}");
                    }
                    Log($"User info set for employee {employeeId} (Name='{employeeName}').");

                    Log($"Attempting to modify privilege for employee {employeeId} to normal user (0)...");
                    bool privOk = sbxpc.SBXPCDLL.ModifyPrivilege(machineNumber, employeeId, 0, 0, 0);
                    if (!privOk)
                    {
                        int sdkErr = 0;
                        try { int tmp; sbxpc.SBXPCDLL.GetLastError(machineNumber, out tmp); sdkErr = tmp; } catch { }
                        if (sdkErr == 5)
                        {
                            Log($"Note: ModifyPrivilege skipped for employee {employeeId} (user not enrolled yet, error 5).");
                        }
                        else
                        {
                            Log($"Warning: ModifyPrivilege failed for employee {employeeId}. SDK GetLastError={sdkErr}");
                        }
                    }
                    else
                    {
                        Log($"Privilege modified for employee {employeeId}.");
                    }

                    Log($"Attempting to enable user {employeeId} on device...");
                    bool enableOk = sbxpc.SBXPCDLL.EnableUser(machineNumber, employeeId, 0, 0, (byte)1);
                    if (!enableOk)
                    {
                        int sdkErr = 0;
                        try { int tmp; sbxpc.SBXPCDLL.GetLastError(machineNumber, out tmp); sdkErr = tmp; } catch { }
                        if (sdkErr == 5)
                        {
                            Log($"Note: EnableUser skipped for employee {employeeId} (user not enrolled yet, error 5).");
                        }
                        else
                        {
                            Log($"Warning: EnableUser failed for employee {employeeId}. SDK GetLastError={sdkErr}");
                        }
                    }
                    else
                    {
                        Log($"User {employeeId} enabled.");
                    }

                    Log("Refreshing device data (commit) by toggling EnableDevice)...");
                    sbxpc.SBXPCDLL.EnableDevice(machineNumber, (byte)0);
                    sbxpc.SBXPCDLL.EnableDevice(machineNumber, (byte)1);
                    Log("Device refreshed");

                    sbxpc.SBXPCDLL.Disconnect(machineNumber);
                    Log($"SetUserInMachine succeeded for employee {employeeId}.");
                }
                catch (Exception ex)
                {
                    Log($"ERROR: SetUserInMachine failed for employee {employeeId}: {ex}");
                    try { sbxpc.SBXPCDLL.Disconnect(machineNumber); } catch { }
                    throw new Exception($"SetUserInMachine failed: {ex.Message}", ex);
                }
                finally
                {
                    Monitor.Exit(SdkLock);
                }
            }
            catch (Exception)
            {
                // This catch is for TryEnter timeout if we moved it inside, but it's outside.
                // Keeping it for outer try structure stability if needed.
                throw;
            }
        }

        public void SetUserEnabled(int employeeId, bool enabled, string deviceIp = null, int devicePort = 0, int machineNumber = 0, int commKey = 0)
        {
            deviceIp = deviceIp ?? DeviceConfig.IpAddress;
            devicePort = devicePort != 0 ? devicePort : DeviceConfig.Port;
            machineNumber = machineNumber != 0 ? machineNumber : DeviceConfig.MachineNumber;
            commKey = commKey != 0 ? commKey : DeviceConfig.CommKey;

            if (!Monitor.TryEnter(SdkLock, TimeSpan.FromSeconds(30)))
            {
                Log("ERROR: SetUserEnabled timed out waiting for SDK lock.");
                throw new Exception("Timed out waiting for biometric SDK access.");
            }

            try
            {
                try
                {
                    if (!SdkInitialized)
                    {
                        sbxpc.SBXPCDLL.DotNET();
                        SdkInitialized = true;
                    }

                    try { sbxpc.SBXPCDLL.Disconnect(machineNumber); } catch { }

                    Log($"Connecting to device {deviceIp}:{devicePort} (machineNumber={machineNumber})...");
                    bool connected = sbxpc.SBXPCDLL.ConnectTcpip(machineNumber, deviceIp, devicePort, commKey);
                    if (!connected)
                    {
                        int lastErr = 0;
                        try { int tmp; sbxpc.SBXPCDLL.GetLastError(machineNumber, out tmp); lastErr = tmp; } catch { }
                        throw new Exception($"Failed to connect to biometric device. SDK GetLastError={lastErr}");
                    }
                    Log("Connected to biometric device.");

                    byte enableFlag = enabled ? (byte)1 : (byte)0;
                    Log($"Setting user {employeeId} enabled={enabled} on device...");
                    bool enableOk = sbxpc.SBXPCDLL.EnableUser(machineNumber, employeeId, 0, 0, enableFlag);
                    if (!enableOk)
                    {
                        int sdkErr = 0;
                        try { int tmp; sbxpc.SBXPCDLL.GetLastError(machineNumber, out tmp); sdkErr = tmp; } catch { }
                        throw new Exception($"EnableUser failed for employee {employeeId}. SDK GetLastError={sdkErr}");
                    }
                    Log($"User {employeeId} {(enabled ? "enabled" : "disabled")} on device.");

                    sbxpc.SBXPCDLL.EnableDevice(machineNumber, (byte)0);
                    sbxpc.SBXPCDLL.EnableDevice(machineNumber, (byte)1);

                    sbxpc.SBXPCDLL.Disconnect(machineNumber);
                    Log($"SetUserEnabled succeeded for employee {employeeId}.");
                }
                catch (Exception ex)
                {
                    Log($"ERROR: SetUserEnabled failed for employee {employeeId}: {ex}");
                    try { sbxpc.SBXPCDLL.Disconnect(machineNumber); } catch { }
                    throw new Exception($"SetUserEnabled failed: {ex.Message}", ex);
                }
                finally
                {
                    Monitor.Exit(SdkLock);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void DeleteUser(int employeeId, string deviceIp = null, int devicePort = 0, int machineNumber = 0, int commKey = 0)
        {
            deviceIp = deviceIp ?? DeviceConfig.IpAddress;
            devicePort = devicePort != 0 ? devicePort : DeviceConfig.Port;
            machineNumber = machineNumber != 0 ? machineNumber : DeviceConfig.MachineNumber;
            commKey = commKey != 0 ? commKey : DeviceConfig.CommKey;

            if (!Monitor.TryEnter(SdkLock, TimeSpan.FromSeconds(30)))
            {
                Log("ERROR: DeleteUser timed out waiting for SDK lock.");
                throw new Exception("Timed out waiting for biometric SDK access.");
            }

            try
            {
                try
                {
                    if (!SdkInitialized)
                    {
                        sbxpc.SBXPCDLL.DotNET();
                        SdkInitialized = true;
                    }

                    try { sbxpc.SBXPCDLL.Disconnect(machineNumber); } catch { }

                    Log($"Connecting to device {deviceIp}:{devicePort} (machineNumber={machineNumber})...");
                    bool connected = sbxpc.SBXPCDLL.ConnectTcpip(machineNumber, deviceIp, devicePort, commKey);
                    if (!connected)
                    {
                        int lastErr = 0;
                        try { int tmp; sbxpc.SBXPCDLL.GetLastError(machineNumber, out tmp); lastErr = tmp; } catch { }
                        throw new Exception($"Failed to connect to biometric device. SDK GetLastError={lastErr}");
                    }
                    Log("Connected to biometric device.");

                    Log($"Deleting user {employeeId} from device...");
                    bool anyDeleted = false;
                    int lastSdkErr = 0;
                    
                    bool deleteOk = sbxpc.SBXPCDLL.DeleteEnrollData(machineNumber, employeeId, machineNumber, 15);
                    if (deleteOk) { anyDeleted = true; Log($"Deleted password enrollment for user {employeeId}."); }
                    else { sbxpc.SBXPCDLL.GetLastError(machineNumber, out lastSdkErr); }
                    
                    for (int fp = 0; fp <= 9; fp++)
                    {
                        if (sbxpc.SBXPCDLL.DeleteEnrollData(machineNumber, employeeId, machineNumber, fp)) { anyDeleted = true; }
                    }
                    
                    if (sbxpc.SBXPCDLL.DeleteEnrollData(machineNumber, employeeId, machineNumber, 11)) { anyDeleted = true; }
                    
                    if (!anyDeleted)
                    {
                        if (lastSdkErr != 4 && lastSdkErr != 5) throw new Exception($"DeleteEnrollData failed for employee {employeeId}. SDK GetLastError={lastSdkErr}");
                        Log($"User {employeeId} not found on device.");
                    }

                    sbxpc.SBXPCDLL.EnableDevice(machineNumber, (byte)0);
                    sbxpc.SBXPCDLL.EnableDevice(machineNumber, (byte)1);
                    sbxpc.SBXPCDLL.Disconnect(machineNumber);
                    Log($"DeleteUser succeeded for employee {employeeId}.");
                }
                catch (Exception ex)
                {
                    Log($"ERROR: DeleteUser failed for employee {employeeId}: {ex}");
                    try { sbxpc.SBXPCDLL.Disconnect(machineNumber); } catch { }
                    throw new Exception($"DeleteUser failed: {ex.Message}", ex);
                }
                finally
                {
                    Monitor.Exit(SdkLock);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool TestConnection(out string errorMessage, string deviceIp = null, int devicePort = 0, int machineNumber = 0, int commKey = 0)
        {
            errorMessage = string.Empty;
            deviceIp = deviceIp ?? DeviceConfig.IpAddress;
            devicePort = devicePort != 0 ? devicePort : DeviceConfig.Port;
            machineNumber = machineNumber != 0 ? machineNumber : DeviceConfig.MachineNumber;
            commKey = commKey != 0 ? commKey : DeviceConfig.CommKey;
            
            if (!Monitor.TryEnter(SdkLock, TimeSpan.FromSeconds(30)))
            {
                errorMessage = "Timed out waiting for SDK lock.";
                return false;
            }

            try
            {
                try
                {
                    if (!SdkInitialized)
                    {
                        sbxpc.SBXPCDLL.DotNET();
                        SdkInitialized = true;
                    }

                    try { sbxpc.SBXPCDLL.Disconnect(machineNumber); } catch { }

                    Log($"Testing connection to {deviceIp}:{devicePort} (machineNumber={machineNumber})...");
                    bool connected = sbxpc.SBXPCDLL.ConnectTcpip(machineNumber, deviceIp, devicePort, commKey);
                    if (!connected)
                    {
                        int lastErr = 0;
                        try { int tmp; sbxpc.SBXPCDLL.GetLastError(machineNumber, out tmp); lastErr = tmp; } catch { }
                        errorMessage = $"Failed to connect. SDK GetLastError={lastErr}";
                        return false;
                    }
                    
                    sbxpc.SBXPCDLL.Disconnect(machineNumber);
                    return true;
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    return false;
                }
                finally
                {
                    Monitor.Exit(SdkLock);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private (bool found, bool ok, string error) TryInvokeBoolSdkMethod(Type sdkType, string[] candidateNames, object[] args)
        {
            foreach (var name in candidateNames)
            {
                MethodInfo[] methods = sdkType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
                foreach (var mi in methods)
                {
                    if (!string.Equals(mi.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
                    var parameters = mi.GetParameters();
                    if (parameters.Length != args.Length) continue;
                    try
                    {
                        object[] invokeArgs = new object[args.Length];
                        for (int i = 0; i < args.Length; i++) invokeArgs[i] = Convert.ChangeType(args[i], parameters[i].ParameterType);
                        var result = mi.Invoke(null, invokeArgs);
                        if (result is bool b) return (true, b, null);
                        if (result is int iv) return (true, iv != 0, null);
                        return (true, true, null);
                    }
                    catch (Exception ex) { return (true, false, ex.Message); }
                }
            }
            return (false, false, "MethodNotFound");
        }
    }
}
