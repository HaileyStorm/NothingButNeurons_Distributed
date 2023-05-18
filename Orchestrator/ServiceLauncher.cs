using NothingButNeurons.Shared.Messages;
using Proto;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using Proto.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media;
using Process = System.Diagnostics.Process;

namespace NothingButNeurons.Orchestrator;

internal class ServiceLauncher
{
    private const string netVersion = "net7.0";
    private const string netVersionWin = $"{netVersion}-windows";

    private ActorSystem ProtoSystem;
    private string BasePath;
    private string WPFBinPath;
    private string ConsoleBinPath;

    internal ServiceLauncher(bool isDebug, ActorSystem protoSystem)
    {
        ProtoSystem = protoSystem;

        BasePath = Path.GetDirectoryName(
                    Path.GetDirectoryName(
                        Path.GetDirectoryName(
                            Path.GetDirectoryName(
                                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))));

        if (isDebug )
        {
            WPFBinPath = "bin\\Debug\\" + netVersionWin;
            ConsoleBinPath = "bin\\Debug\\" + netVersion;
        } else
        {
            WPFBinPath = "bin\\Release\\" + netVersionWin;
            ConsoleBinPath = "bin\\Release\\" + netVersion;
        }
    }

    internal async Task Launch(Service service)
    {
        string arguments = "";

        if (!string.Equals(service.ProjectName, "SettingsMonitor", StringComparison.InvariantCultureIgnoreCase))
        {
            int? servicePort = await Nodes.GetPortFromSettings(ProtoSystem.Root, service.ProjectName);

            if (servicePort == null)
            {
                CCSL.Console.CombinedWriteLine($"Error launching service {service.Name}: unable to retrieve service port from settings.");
                service.StatusColor = Brushes.Red;
                return;
            }

            arguments = $"{servicePort}";
        }
        
        try
        {
            // Create a new ProcessStartInfo object with the executable path
            string servicePath = Path.Combine(BasePath, service.ProjectName, service.IsWPF ? WPFBinPath : ConsoleBinPath, $"NothingButNeurons.{service.ProjectName}.exe");
            CCSL.Console.CombinedWriteLine($"\nLaunching service: {servicePath} {arguments}\n");
            ProcessStartInfo startInfo = new ProcessStartInfo(servicePath)
            {
                Arguments = service.IsWPF ? $"\"{arguments}\"" : arguments
            };

            // Start the process.
            // Note: don't try to monitor this process, or update status light to anything other than green here - just let ServiceMonitor handle that.
            Process.Start(startInfo);

            service.StatusColor = Brushes.Green;
            // Preventing launching another copy (not that this is *always* a problem, but it's unnecessary for sure, at least for now)
            service.Enabled = false;
        }
        catch (Exception ex)
        {
            CCSL.Console.CombinedWriteLine($"Error launching service {service.Name}: {ex.Message}");
            service.StatusColor = Brushes.Red;
        }
    }

    internal void Kill(Service service)
    {
        try
        {
            // Get the name of the executable without the .exe extension
            string processName = $"NothingButNeurons.{service.ProjectName}";

            // Find all running instances of the specified executable
            Process[] processes = Process.GetProcessesByName(processName);

            // Kill each instance of the process
            foreach (Process process in processes)
            {
                process.Kill();
            }
        }
        catch (Exception ex)
        {
            CCSL.Console.CombinedWriteLine($"Error killing service {service.Name}: {ex.Message}");
        }
    }
}
