using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Timers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using System.Net;

namespace ProcessMonitorService
{
    public partial class ProcessMonitorService : ServiceBase
    {
        private Timer refreshTimer;
        private Timer configTimer;
        private bool isShuttingDown = false;
        private string baseDir;
        private string procListPath;
        private string urlConfigPath;
        private string procProtectPath;
        private int nTime = 0;

        public ProcessMonitorService()
        {
            InitializeComponent();
            this.CanShutdown = true;
            this.CanStop = true;
            this.AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            baseDir = AppDomain.CurrentDomain.BaseDirectory;
            procListPath = Path.Combine(baseDir, "proclist.txt");
            urlConfigPath = Path.Combine(baseDir, "urlcfg.txt");
            procProtectPath = Path.Combine(baseDir, "procprotect.txt");

            // Set up timers
            refreshTimer = new Timer(500); // 500 ms
            refreshTimer.Elapsed += OnRefreshTimerElapsed;
            refreshTimer.Start();

            configTimer = new Timer(11000); // 11000 ms (1000 * 11)
            configTimer.Elapsed += OnConfigTimerElapsed;
            configTimer.Start();

            EventLog.WriteEntry("Process Monitor Service started.", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            refreshTimer.Stop();
            configTimer.Stop();
            EventLog.WriteEntry("Process Monitor Service stopped.", EventLogEntryType.Information);
        }

        protected override void OnShutdown()
        {
            isShuttingDown = true;
            OnStop();
            base.OnShutdown();
            EventLog.WriteEntry("System is shutting down.", EventLogEntryType.Information);
        }

        private void OnConfigTimerElapsed(object sender, ElapsedEventArgs e)
        {
            DownloadConfigFile();
        }

        private void OnRefreshTimerElapsed(object sender, ElapsedEventArgs e)
        {
            nTime++;

            if (isShuttingDown)
                return;

            MonitorProcesses();
            RemoveUnauthorizedUsers();
        }

        private void DownloadConfigFile()
        {
            if (File.Exists(urlConfigPath))
            {
                try
                {
                    string url = File.ReadAllText(urlConfigPath).Trim();
                    if (!string.IsNullOrEmpty(url))
                    {
                        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                        {
                            string destFile = procListPath;

                            // Clear cache for the URL
                            DeleteUrlCacheEntry(url);

                            using (var client = new WebClient())
                            {
                                client.DownloadFile(url, destFile);
                            }

                            EventLog.WriteEntry($"Downloaded configuration from {url}.", EventLogEntryType.Information);
                        }
                        else
                        {
                            EventLog.WriteEntry($"Invalid URL in urlcfg.txt: {url}", EventLogEntryType.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry($"Failed to download configuration: {ex.Message}", EventLogEntryType.Error);
                }
            }
        }

        private void MonitorProcesses()
        {
            // Read the list of protected processes
            var protectedProcesses = new List<string>();
            if (File.Exists(procProtectPath))
            {
                protectedProcesses = File.ReadAllLines(procProtectPath).Select(p => p.Trim()).ToList();
            }

            // Read the list of forbidden processes
            var forbiddenProcesses = new List<string>();
            if (File.Exists(procListPath))
            {
                forbiddenProcesses = File.ReadAllLines(procListPath).Select(p => p.Trim()).ToList();
            }

            // Get all running processes
            var runningProcesses = Process.GetProcesses();
            var processNames = runningProcesses.Select(p => p.ProcessName).ToList();

            // Monitor protected processes
            bool allProtectedRunning = true;
            foreach (var procName in protectedProcesses)
            {
                string processName = Path.GetFileNameWithoutExtension(procName);
                var procExists = processNames.Any(p => p.Equals(processName, StringComparison.OrdinalIgnoreCase));

                if (!procExists)
                {
                    allProtectedRunning = false;
                    break;
                }
                else
                {
                    // Check if the process is suspended
                    var proc = runningProcesses.FirstOrDefault(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
                    if (proc != null && IsProcessSuspended(proc))
                    {
                        allProtectedRunning = false;
                        break;
                    }
                }
            }

            if (!allProtectedRunning && nTime > 80)
            {
                // Reboot the system
                EnableShutdownPrivilege();
                RebootSystem(true);
                return;
            }

            // Reboot if network is not alive after certain time
            if (!IsNetworkAlive() && nTime > 120)
            {
                EnableShutdownPrivilege();
                RebootSystem(true);
                return;
            }

            // Kill forbidden processes
            foreach (var proc in runningProcesses)
            {
                if (forbiddenProcesses.Contains(proc.ProcessName, StringComparer.OrdinalIgnoreCase))
                {
                    try
                    {
                        proc.Kill();
                        EventLog.WriteEntry($"Killed forbidden process: {proc.ProcessName}", EventLogEntryType.Warning);
                    }
                    catch (Exception ex)
                    {
                        EventLog.WriteEntry($"Failed to kill process {proc.ProcessName}: {ex.Message}", EventLogEntryType.Error);
                    }
                }
            }
        }

        private void RemoveUnauthorizedUsers()
        {
            // Read the list of authorized users
            string userListPath = Path.Combine(baseDir, "userlist.txt");
            var authorizedUsers = new List<string>();
            if (File.Exists(userListPath))
            {
                authorizedUsers = File.ReadAllLines(userListPath).Select(u => u.Trim()).ToList();
            }

            // Get all local users
            var allUsers = GetLocalUsers();

            // Remove unauthorized users
            foreach (var user in allUsers)
            {
                if (!authorizedUsers.Contains(user, StringComparer.OrdinalIgnoreCase))
                {
                    RemoveUser(user);
                    EventLog.WriteEntry($"Removed unauthorized user: {user}", EventLogEntryType.Warning);
                }
            }
        }

        private List<string> GetLocalUsers()
        {
            var users = new List<string>();
            try
            {
                using (var context = new System.DirectoryServices.AccountManagement.PrincipalContext(System.DirectoryServices.AccountManagement.ContextType.Machine))
                {
                    var userPrincipal = new System.DirectoryServices.AccountManagement.UserPrincipal(context);
                    using (var searcher = new System.DirectoryServices.AccountManagement.PrincipalSearcher(userPrincipal))
                    {
                        foreach (var result in searcher.FindAll())
                        {
                            users.Add(result.SamAccountName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry($"Error retrieving local users: {ex.Message}", EventLogEntryType.Error);
            }

            return users;
        }

        private void RemoveUser(string username)
        {
            try
            {
                var psi = new ProcessStartInfo("net", $"user {username} /delete")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                var process = Process.Start(psi);
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry($"Failed to remove user {username}: {ex.Message}", EventLogEntryType.Error);
            }
        }

        private bool IsNetworkAlive()
        {
            int flags = 0;
            return InternetGetConnectedState(ref flags, 0);
        }

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetGetConnectedState(ref int lpdwFlags, int dwReserved);

        private bool IsProcessSuspended(Process process)
        {
            bool isSuspended = true;
            try
            {
                foreach (ProcessThread pT in process.Threads)
                {
                    IntPtr hThread = OpenThread(ThreadAccess.SUSPEND_RESUME | ThreadAccess.QUERY_INFORMATION, false, (uint)pT.Id);
                    if (hThread == IntPtr.Zero)
                        continue;

                    uint suspendCount = SuspendThread(hThread);
                    if (suspendCount >= 0)
                    {
                        // Thread is not suspended
                        ResumeThread(hThread);
                        isSuspended = false;
                        CloseHandle(hThread);
                        break;
                    }
                    CloseHandle(hThread);
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry($"Error checking if process is suspended: {ex.Message}", EventLogEntryType.Error);
            }
            return isSuspended;
        }

        private void RebootSystem(bool force)
        {
            if (isShuttingDown)
            {
                return;
            }

            EnableShutdownPrivilege();

            // Reboot the system
            int flags = EWX_REBOOT | (force ? EWX_FORCE : 0);
            ExitWindowsEx(flags, 0);
        }

        private void EnableShutdownPrivilege()
        {
            IntPtr hToken;
            OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out hToken);

            LUID luid;
            LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, out luid);

            TOKEN_PRIVILEGES tp;
            tp.PrivilegeCount = 1;
            tp.Privileges = new LUID_AND_ATTRIBUTES[1];
            tp.Privileges[0].Luid = luid;
            tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

            AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);

            CloseHandle(hToken);
        }

        // P/Invoke declarations
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool LookupPrivilegeValue(string lpsystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public LUID_AND_ATTRIBUTES[] Privileges;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint TOKEN_QUERY = 0x0008;
        private const int SE_PRIVILEGE_ENABLED = 0x00000002;
        private const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";

        private const int EWX_LOGOFF = 0x00000000;
        private const int EWX_SHUTDOWN = 0x00000001;
        private const int EWX_REBOOT = 0x00000002;
        private const int EWX_FORCE = 0x00000004;
        private const int SHTDN_REASON_MAJOR_OTHER = 0x00000000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ExitWindowsEx(int uFlags, int dwReason);

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern long DeleteUrlCacheEntry(string lpszUrlName);

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);

        [Flags]
        public enum ThreadAccess : int
        {
            SUSPEND_RESUME = (0x0002),
            QUERY_INFORMATION = (0x0040)
        }
    }
}