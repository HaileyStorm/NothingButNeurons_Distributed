using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NothingButNeurons.Orchestrator;

internal class ServiceLauncher
{
    private const string netVersion = "net7.0";
    private const string netVersionWin = $"{netVersion}-windows";
    
    private string BasePath;
    private string WPFBinPath;
    private string ConsoleBinPath;

    internal ServiceLauncher(bool isDebug)
    {
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

    internal void Launch(Service service)
    {
        int DebugServerPort = MainWindow.Instance.GetPort("DebugServer");
        int IOPort = MainWindow.Instance.GetPort("IO");

        string arguments = "";
        switch (service.ProjectName) {
            case "IO":
                arguments = $"{service.Port} {DebugServerPort}";
                break;
            case "DebugServer":
                arguments = $"{service.Port}";
                break;
            case "DebugFileWriter":
                arguments = $"{service.Port} {DebugServerPort}";
                break;
            case "DebugLogViewer":
                arguments = $"{service.Port} {DebugServerPort}";
                break;
            case "Visualizer":
                arguments = $"{service.Port} {DebugServerPort}";
                break;
            case "Designer":
                arguments = $"{service.Port} {IOPort}";
                break;
            default:
                throw new ArgumentException("Service project name unexpected, unable to configure/launch.");
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
