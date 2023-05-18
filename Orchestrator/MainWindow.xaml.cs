global using NothingButNeurons.Shared;
global using NothingButNeurons.Shared.Messages;
global using NothingButNeurons.CCSL;
global using System.Diagnostics;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using Proto;
using Proto.Remote;

namespace NothingButNeurons.Orchestrator;

public partial class MainWindow : Window
{
    internal static MainWindow Instance { get; private set; }
    internal const int ServiceMonitorPort = Shared.Consts.DefaultPorts.ORCHESTRATOR_MONITOR;
    internal const int SettingsMonitorPort = Shared.Consts.DefaultPorts.SETTINGS_MONITOR;

    internal ActorSystem ProtoSystem;

    internal ServiceLauncher ServiceLauncher { get; private set; }
    internal ServiceMonitor ServiceMonitor { get; private set; }

    public ObservableCollection<Service> Services { get; set; } = new ObservableCollection<Service>
    {
        new Service { Name = "Settings Monitor", ProjectName="SettingsMonitor", ActorName="SettingsMonitor", PreReqProjects = Array.Empty<string>(), IsWPF=false, Port = Shared.Consts.DefaultPorts.SETTINGS_MONITOR, StatusColor = Brushes.Gray },
        new Service { Name = "IO / HiveMind", ProjectName="IO", ActorName="HiveMind", PreReqProjects = new string[] { "SettingsMonitor" }, IsWPF=false, Port = Shared.Consts.DefaultPorts.IO, StatusColor = Brushes.Gray },
        // SettingsMonitor is prereq because otherwise DebugServer can never report its online status / PID, so others will never nr updated when it comes online
        new Service { Name = "Debug Server", ProjectName="DebugServer", ActorName="DebugServer", PreReqProjects = new string[] { "SettingsMonitor" }, IsWPF=false, Port = Shared.Consts.DefaultPorts.DEBUG_SERVER, StatusColor = Brushes.Gray },
        new Service { Name = "Debug File Writer", ProjectName="DebugFileWriter", ActorName="DebugFileWriter", PreReqProjects = new string[] { "SettingsMonitor", "DebugServer" }, IsWPF=false, Port = Shared.Consts.DefaultPorts.DEBUG_FILE_WRITER, StatusColor = Brushes.Gray },
        new Service { Name = "Debug Log Viewer", ProjectName="DebugLogViewer", ActorName="DebugUI", PreReqProjects = new string[] { "SettingsMonitor", "DebugServer" }, IsWPF=true, Port = Shared.Consts.DefaultPorts.DEBUG_LOG_VIEWER, StatusColor = Brushes.Gray },
        // Currently there's nothing to visualize unless a brain is spawned and random inputs are sent, which happens via Designer - but a) that's not how things will be, b) Visualizer can be open before or after Designer, so long as it's own before the Designer Spawn button is clicked
        // Technically IO isn't required either...
        new Service { Name = "Visualizer", ProjectName="Visualizer", ActorName="NetworkVisualizationUpdater", PreReqProjects = new string[] { "SettingsMonitor", "DebugServer", "IO" }, IsWPF=true, Port = Shared.Consts.DefaultPorts.VISUALIZER, StatusColor = Brushes.Gray },
        // Visualizer must be open before Spawn clicked in Designer now if want Visualizer to catch it, but in order to create Brains or send randomized input Designer does not require Visualizer, so it's not on the prereq list
        new Service { Name = "Designer", ProjectName="Designer", ActorName="DesignerHelper", PreReqProjects = new string[] { "SettingsMonitor", "IO" }, IsWPF=true, Port = Shared.Consts.DefaultPorts.DESIGNER, StatusColor = Brushes.Gray }
    };

    public MainWindow()
    {
        bool isDebug;
        #if DEBUG
            isDebug = true;
        #else
            isDebug = false;
        #endif

        Instance = this;

        ProtoSystem = Nodes.GetActorSystem(ServiceMonitorPort);

        ServiceLauncher = new(isDebug);
        ServiceMonitor = new();

        // Register the converter in the resources
        this.Resources.Add("IndexToRowConverter", new IndexToRowConverter());
        InitializeComponent();
        DataContext = this;

        Nodes.SendNodeOnline(ProtoSystem.Root, "Orchestrator", ProtoSystem.Root.Self);

        // Kill launched processes on close. Doesn't work for stop button in VS, but it's still more robust than the Closing event.
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private void LaunchButton_Click(object sender, RoutedEventArgs e)
    {
        // Identify the service associated with the clicked button
        var clickedButton = (Button)sender;
        var service = (Service)clickedButton.DataContext;

        // Perform the desired action for launching the service
        ServiceLauncher.Launch(service);
    }

    public class IndexToRowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                // Add 1 to the index to account for the header row in the Grid
                return index + 1;
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    private void OnProcessExit(object sender, EventArgs e)
    {
        foreach (var service in Services.OrderByDescending(s => s.PreReqProjects.Length))
        {
            CCSL.Console.CombinedWriteLine($"Killing {service.ProjectName}");
            Nodes.SendNodeOffline(ProtoSystem.Root, service.ProjectName);
            if (string.Equals(service.ProjectName, "SettingsMonitor", StringComparison.InvariantCultureIgnoreCase))
                System.Threading.Thread.Sleep(500);
            ServiceLauncher.Kill(service);
        }

        CCSL.Console.CombinedWriteLine("NothingButNeurons.Orchestrator program shutting down...");
        Nodes.SendNodeOffline(ProtoSystem.Root, "Orchestrator");
        ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
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

public class Service : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private string _name;
    public string Name
    {
        get { return _name; }
        set { _name = value; OnPropertyChanged(); }
    }

    private string _projectName;
    public string ProjectName
    {
        get { return _projectName; }
        set { _projectName = value; OnPropertyChanged(); }
    }

    private string _actorName;
    public string ActorName
    {
        get { return _actorName; }
        set { _actorName = value; OnPropertyChanged(); }
    }

    private string[] _preReqProjects;
    public string[] PreReqProjects
    {
        get { return _preReqProjects; }
        set { _preReqProjects = value; OnPropertyChanged(); }
    }

    private bool _isWPF;
    public bool IsWPF
    {
        get { return _isWPF; }
        set { _isWPF = value; OnPropertyChanged(); }
    }

    private int _port;
    public int Port
    {
        get { return _port; }
        set { _port = value; OnPropertyChanged(); }
    }

    private SolidColorBrush _statusColor;
    public SolidColorBrush StatusColor
    {
        get { return _statusColor; }
        set { _statusColor = value; OnPropertyChanged(); }
    }

    private bool _enabled = false;
    public bool Enabled
    {
        get { return _enabled; }
        set { _enabled = value; OnPropertyChanged(); }
    }
}