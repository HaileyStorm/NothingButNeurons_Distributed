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
using NothingButNeurons.Shared.Consts;

namespace NothingButNeurons.Orchestrator;

public partial class MainWindow : Window
{
    internal static MainWindow Instance { get; private set; }
    internal const int SettingsMonitorPort = Shared.Consts.DefaultPorts.SETTINGS_MONITOR;

    internal ActorSystem ProtoSystem;

    internal ServiceLauncher ServiceLauncher { get; private set; }
    internal PID ServiceMonitor { get; private set; }

    public ObservableCollection<Service> Services { get; set; } = new ObservableCollection<Service>
    {
        new Service { Name = "Settings Monitor", ProjectName="SettingsMonitor", PreReqProjects = Array.Empty<string>(), IsWPF=false, StatusColor = Brushes.Gray },
        new Service { Name = "IO / HiveMind", ProjectName="IO",  PreReqProjects = new string[] { "SettingsMonitor" }, IsWPF=false, StatusColor = Brushes.Gray },
        // SettingsMonitor is prereq because otherwise DebugServer can never report its online status / PID, so others will never nr updated when it comes online
        new Service { Name = "Debug Server", ProjectName="DebugServer", PreReqProjects = new string[] { "SettingsMonitor" }, IsWPF=false, StatusColor = Brushes.Gray },
        new Service { Name = "Debug File Writer", ProjectName="DebugFileWriter", PreReqProjects = new string[] { "SettingsMonitor", "DebugServer" }, IsWPF=false, StatusColor = Brushes.Gray },
        new Service { Name = "Debug Log Viewer", ProjectName="DebugLogViewer", PreReqProjects = new string[] { "SettingsMonitor", "DebugServer" }, IsWPF=true, StatusColor = Brushes.Gray },
        // Currently there's nothing to visualize unless a brain is spawned and random inputs are sent, which happens via Designer - but a) that's not how things will be, b) Visualizer can be open before or after Designer, so long as it's own before the Designer Spawn button is clicked
        // Technically IO isn't required either...
        new Service { Name = "Visualizer", ProjectName="Visualizer", PreReqProjects = new string[] { "SettingsMonitor", "DebugServer", "IO" }, IsWPF=true, StatusColor = Brushes.Gray },
        // Visualizer must be open before Spawn clicked in Designer now if want Visualizer to catch it, but in order to create Brains or send randomized input Designer does not require Visualizer, so it's not on the prereq list
        new Service { Name = "Designer", ProjectName="Designer", PreReqProjects = new string[] { "SettingsMonitor", "IO" }, IsWPF=true, StatusColor = Brushes.Gray }
    };

    public MainWindow()
    {
        Instance = this;

        // Register the converter in the resources
        this.Resources.Add("IndexToRowConverter", new IndexToRowConverter());
        DataContext = this;
        InitializeComponent();

        InitializeActorSystem();

        // Kill launched processes on close. Doesn't work for stop button in VS, but it's still more robust than the Closing event.
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private async void InitializeActorSystem()
    {
        bool isDebug;
#if DEBUG
        isDebug = true;
#else
            isDebug = false;
#endif

        ProtoSystem = Nodes.GetActorSystem(Shared.Consts.DefaultPorts.ORCHESTRATOR_MONITOR);
        ServiceLauncher = new(isDebug, ProtoSystem);
        Service settingsService = Services.Where(s => string.Equals(s.ProjectName, "SettingsMonitor", StringComparison.InvariantCultureIgnoreCase)).Single();
        await ServiceLauncher.Launch(settingsService);
        int attempts = 0;
        int? servicePort = null;
        while (attempts < 20)
        {
            try
            {
                servicePort = await Nodes.GetPortFromSettings(ProtoSystem.Root, "OrchestratorMonitor");
                if (servicePort != null)
                    break;
            }
            finally
            {
                System.Threading.Thread.Sleep(200);
                attempts++;
            }
        }
        if (servicePort != null && servicePort != Shared.Consts.DefaultPorts.ORCHESTRATOR_MONITOR)
        {
            CCSL.Console.CombinedWriteLine($"OrchestratorMonitor port from settings doesn't match default; restarting with port from settings: {servicePort}");
            //ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
            ProtoSystem = Nodes.GetActorSystem((int)servicePort!);
            ServiceLauncher = new(isDebug, ProtoSystem);
        }

        ServiceMonitor = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new ServiceMonitor(null)), "OrchestratorMonitor");
        settingsService.StatusColor = Brushes.Green;
        settingsService.Enabled = false;
        // Will be replaced by the real value with SettingChangedMessage
        settingsService.PID = PID.FromAddress($"127.0.0.1:{DefaultPorts.SETTINGS_MONITOR}", "SettingsMonitor").ToString();

        Nodes.SendNodeOnline(ProtoSystem.Root, "Orchestrator", ServiceMonitor);
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

    private async void OnProcessExit(object sender, EventArgs e)
    {
        CCSL.Console.CombinedWriteLine("NothingButNeurons.Orchestrator program shutting down...");
        Nodes.SendNodeOffline(ProtoSystem.Root, "Orchestrator");

        foreach (var service in Services.OrderByDescending(s => s.PreReqProjects.Length))
        {
            CCSL.Console.CombinedWriteLine($"Killing {service.ProjectName}");
            Nodes.SendNodeOffline(ProtoSystem.Root, service.ProjectName);
            if (string.Equals(service.ProjectName, "SettingsMonitor", StringComparison.InvariantCultureIgnoreCase))
                System.Threading.Thread.Sleep(500);
            ServiceLauncher.Kill(service);
        }

        ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
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

    private string? _pid;
    public string? PID
    {
        get { return _pid; }
        set { _pid = value; OnPropertyChanged(); }
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