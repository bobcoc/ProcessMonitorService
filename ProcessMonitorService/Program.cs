using System.ServiceProcess;

namespace ProcessMonitorService
{
    static class Program
    {
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ProcessMonitorService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}