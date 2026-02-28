using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using System.Xml;

namespace Z903AttendanceService
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            // If command-line arguments provided, run config editor mode
            if (args.Length > 0)
            {
                RunConfigEditor(args);
                return;
            }

            // Normal service mode
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new AttendanceService()
            };
            ServiceBase.Run(ServicesToRun);
        }

        /// <summary>
        /// Simple config editor for updating device settings
        /// </summary>
        static void RunConfigEditor(string[] args)
        {
            string command = args[0].ToLower();

            switch (command)
            {
                case "--help":
                case "-h":
                case "/?":
                    ShowHelp();
                    break;

                case "--show":
                case "-s":
                    ShowCurrentConfig();
                    break;

                case "--set-ip":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Error: Please provide an IP address.");
                        Console.WriteLine("Usage: Z903AttendanceService.exe --set-ip 192.168.1.100");
                    }
                    else
                    {
                        SetConfigValue("DeviceIp", args[1]);
                    }
                    break;

                case "--set-port":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Error: Please provide a port number.");
                        Console.WriteLine("Usage: Z903AttendanceService.exe --set-port 5005");
                    }
                    else
                    {
                        if (int.TryParse(args[1], out int port) && port > 0 && port <= 65535)
                        {
                            SetConfigValue("DevicePort", args[1]);
                        }
                        else
                        {
                            Console.WriteLine("Error: Invalid port number. Must be between 1 and 65535.");
                        }
                    }
                    break;

                case "--test":
                case "-t":
                    TestConnection();
                    break;

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    ShowHelp();
                    break;
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Z903 Attendance Service - Config Editor");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine("Usage: Z903AttendanceService.exe [command] [value]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  --help, -h       Show this help message");
            Console.WriteLine("  --show, -s       Show current configuration");
            Console.WriteLine("  --set-ip <ip>    Set device IP address");
            Console.WriteLine("  --set-port <port> Set device port number");
            Console.WriteLine("  --test, -t       Test device connection");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  Z903AttendanceService.exe --show");
            Console.WriteLine("  Z903AttendanceService.exe --set-ip 192.168.1.100");
            Console.WriteLine("  Z903AttendanceService.exe --set-port 4370");
            Console.WriteLine("  Z903AttendanceService.exe --test");
            Console.WriteLine();
        }

        static void ShowCurrentConfig()
        {
            try
            {
                string ip = ConfigurationManager.AppSettings["DeviceIp"] ?? "Not set";
                string port = ConfigurationManager.AppSettings["DevicePort"] ?? "Not set";
                string connStr = ConfigurationManager.ConnectionStrings["AttendanceDB"]?.ConnectionString ?? "Not set";

                // Mask password in connection string for display
                string maskedConnStr = MaskPassword(connStr);

                Console.WriteLine();
                Console.WriteLine("Current Configuration");
                Console.WriteLine("=====================");
                Console.WriteLine($"  Device IP:   {ip}");
                Console.WriteLine($"  Device Port: {port}");
                Console.WriteLine($"  Database:    {maskedConnStr}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading configuration: {ex.Message}");
            }
        }

        static string MaskPassword(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return connectionString;

            // Simple password masking for display
            var parts = connectionString.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Trim().StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase) ||
                    parts[i].Trim().StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
                {
                    var keyValue = parts[i].Split('=');
                    if (keyValue.Length > 1 && !string.IsNullOrEmpty(keyValue[1]))
                    {
                        parts[i] = keyValue[0] + "=****";
                    }
                }
            }
            return string.Join(";", parts);
        }

        static void SetConfigValue(string key, string value)
        {
            try
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string configPath = exePath + ".config";

                if (!File.Exists(configPath))
                {
                    Console.WriteLine($"Error: Config file not found at {configPath}");
                    return;
                }

                XmlDocument doc = new XmlDocument();
                doc.Load(configPath);

                XmlNode appSettings = doc.SelectSingleNode("//appSettings");
                if (appSettings == null)
                {
                    Console.WriteLine("Error: appSettings section not found in config file.");
                    return;
                }

                XmlNode node = appSettings.SelectSingleNode($"add[@key='{key}']");
                if (node != null)
                {
                    string oldValue = node.Attributes["value"].Value;
                    node.Attributes["value"].Value = value;
                    doc.Save(configPath);

                    Console.WriteLine();
                    Console.WriteLine($"Configuration Updated");
                    Console.WriteLine($"  {key}: {oldValue} -> {value}");
                    Console.WriteLine();
                    Console.WriteLine("Note: Restart the service for changes to take effect.");
                    Console.WriteLine("  net stop Z903AttendanceService");
                    Console.WriteLine("  net start Z903AttendanceService");
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine($"Error: Setting '{key}' not found in config file.");
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Error: Access denied. Run as Administrator to modify configuration.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating configuration: {ex.Message}");
            }
        }

        static void TestConnection()
        {
            string ip = ConfigurationManager.AppSettings["DeviceIp"] ?? "";
            string portStr = ConfigurationManager.AppSettings["DevicePort"] ?? "0";

            if (string.IsNullOrEmpty(ip) || !int.TryParse(portStr, out int port))
            {
                Console.WriteLine("Error: Invalid device configuration. Use --show to view current settings.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"Testing connection to device at {ip}:{port}...");

            const int machineNumber = 1;

            try
            {
                // Initialize the SDK
                sbxpc.SBXPCDLL.DotNET();

                bool connected = sbxpc.SBXPCDLL.ConnectTcpip(machineNumber, ip, port, 0);

                if (connected)
                {
                    Console.WriteLine("SUCCESS: Connected to device!");
                    
                    // Try to get device status values
                    // Status codes: 1=UserCount, 2=FPCount, 3=LogCount (depends on device)
                    if (sbxpc.SBXPCDLL.GetDeviceStatus(machineNumber, 1, out uint userCount))
                    {
                        Console.WriteLine($"  Users: {userCount}");
                    }
                    if (sbxpc.SBXPCDLL.GetDeviceStatus(machineNumber, 2, out uint fpCount))
                    {
                        Console.WriteLine($"  Fingerprints: {fpCount}");
                    }
                    if (sbxpc.SBXPCDLL.GetDeviceStatus(machineNumber, 3, out uint logCount))
                    {
                        Console.WriteLine($"  Attendance Logs: {logCount}");
                    }

                    sbxpc.SBXPCDLL.Disconnect(machineNumber);
                }
                else
                {
                    Console.WriteLine("FAILED: Could not connect to device");
                    Console.WriteLine("  - Check if the device is powered on");
                    Console.WriteLine("  - Verify the IP address and port");
                    Console.WriteLine("  - Ensure network connectivity");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED: {ex.Message}");
            }
            Console.WriteLine();
        }
    }
}
