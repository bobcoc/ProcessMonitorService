using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.ServiceProcess;

namespace ProcessMonitorService
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller processInstaller;
        private ServiceInstaller serviceInstaller;

        public ProjectInstaller()
        {
            processInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

            // Set the Service Account Information
            processInstaller.Account = ServiceAccount.LocalSystem;

            // Set the Service Information
            serviceInstaller.ServiceName = "ProcessMonitorService";
            serviceInstaller.DisplayName = "Process Monitor Service";
            serviceInstaller.Description = "Monitors and manages processes on student computers.";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            this.Installers.Add(serviceInstaller);
            this.Installers.Add(processInstaller);
            if (!EventLog.SourceExists("ProcessMonitorService"))
            {
                EventLog.CreateEventSource("ProcessMonitorService", "Application");
            }
        }
    }
}