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

    internal ServiceLauncher(bool isDebug)
    {
        ProtoSystem = MainWindow.Instance.ProtoSystem;

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

    internal async void Launch(Service service)
    {
        string arguments = "";

        if (service.ProjectName != "SettingsMonitor")
        {
            int? DebugServerPort = await Nodes.GetPortFromSettings(ProtoSystem.Root, "DebugServer");
            //DebugServerPort = MainWindow.Instance.GetPort("DebugServer");
            int? IOPort = await Nodes.GetPortFromSettings(ProtoSystem.Root, "IO");
            //int? IOPort = MainWindow.Instance.GetPort("IO");
            int? servicePort = await Nodes.GetPortFromSettings(ProtoSystem.Root, service.ProjectName);
            //int? servicePort = service.Port;

            if (servicePort == null || DebugServerPort == null || IOPort == null)
            {
                Debug.WriteLine($"Error launching service {service.Name}: unable to retrieve DebugServerPort ({DebugServerPort}) or IOPort ({IOPort}).");
                service.StatusColor = Brushes.Red;
            }

            switch (service.ProjectName)
            {
                case "SettingsMonitor":
                    break;
                case "IO":
                    if (MainWindow.Instance.Services.Where(s => string.Equals(s.ProjectName, "DebugServer", StringComparison.InvariantCultureIgnoreCase)).First().StatusColor == Brushes.Green)
                    {
                        arguments = $"{servicePort} {DebugServerPort}";
                    }
                    else
                    {
                        arguments = $"{servicePort}";
                    }
                    break;
                case "DebugServer":
                    arguments = $"{servicePort}";
                    break;
                case "DebugFileWriter":
                    arguments = $"{servicePort} {DebugServerPort}";
                    break;
                case "DebugLogViewer":
                    arguments = $"{servicePort} {DebugServerPort}";
                    break;
                case "Visualizer":
                    arguments = $"{servicePort} {DebugServerPort}";
                    break;
                case "Designer":
                    arguments = $"{servicePort} {IOPort}";
                    break;
                default:
                    throw new ArgumentException("Service project name unexpected, unable to configure/launch.");
            }
        }
        
        try
        {
            // Create a new ProcessStartInfo object with the executable path
            string servicePath = Path.Combine(BasePath, service.ProjectName, service.IsWPF ? WPFBinPath : ConsoleBinPath, $"NothingButNeurons.{service.ProjectName}.exe");
            Debug.WriteLine($"\nLaunching service: {servicePath} {arguments}\n");
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
            Debug.WriteLine($"Error launching service {service.Name}: {ex.Message}");
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
            Debug.WriteLine($"Error killing service {service.Name}: {ex.Message}");
        }
    }
}
