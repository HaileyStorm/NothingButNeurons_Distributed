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
    
    private ObservableCollection<Service> Services;
    private string BasePath;
    private string WPFBinPath;
    private string ConsoleBinPath;

    internal ServiceLauncher(bool isDebug)
    {
        Services = MainWindow.Instance.Services;

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
        int DebugServerPort = GetPort("DebugServer");

        string arguments = "";
        switch (service.ProjectName) {
            case "IO":
                arguments = $"{service.Port} {DebugServerPort}";
                break;
            case "DebugServer":
                break;
            case "DebugFileWriter":
                break;
            case "DebugLogViewer":
                break;
            case "Visualizer":
                break;
            case "Designer":
                break;
            default:
                throw new ArgumentException("Service project name unexpected, unable to configure/launch.");
        }
        
        try
        {
            // Create a new ProcessStartInfo object with the executable path
            ProcessStartInfo startInfo = new ProcessStartInfo(Path.Combine(BasePath, service.ProjectName, service.IsWPF ? WPFBinPath : ConsoleBinPath, $"NothingButNeurons.{service.ProjectName}.exe"))
            {
                Arguments = arguments
            };

            // Start the process
            Process.Start(startInfo);

            service.StatusColor = Brushes.Green;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error launching service {service.Name}: {ex.Message}");
            service.StatusColor = Brushes.Red;
        }



        // Read the current port value from the input box
        int currentPort = service.Port;

        // Example service launching code
        // Replace with the actual code to launch and monitor the service
        if (service.StatusColor != Brushes.Green)
        {
            service.StatusColor = Brushes.Green;
            // Launch the service using the current port number
        }
    }

    internal int GetPort(Service service)
    {
        return service.Port;
    }
    internal int GetPort(string ProjectName)
    {
        return Services.Where(s => s.ProjectName == ProjectName).First().Port;
    }
}
