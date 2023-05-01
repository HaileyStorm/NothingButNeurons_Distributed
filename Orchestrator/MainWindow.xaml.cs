﻿global using NothingButNeurons.Shared;
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

namespace NothingButNeurons.Orchestrator;

public partial class MainWindow : Window
{
    internal static MainWindow Instance { get; private set; }
    
    internal ServiceLauncher ServiceLauncher { get; private set; }
    internal ServiceMonitor ServiceMonitor { get; private set; }

    public ObservableCollection<Service> Services { get; set; } = new ObservableCollection<Service>
    {
        new Service { Name = "IO / HiveMind", ProjectName="IO", IsWPF=false, Port = 8000, StatusColor = Brushes.Gray },
        new Service { Name = "Debug Server", ProjectName="DebugServer", IsWPF=false, Port = 8001, StatusColor = Brushes.Gray },
        new Service { Name = "Debug File Writer", ProjectName="DebugFileWriter", IsWPF=false, Port = 8002, StatusColor = Brushes.Gray },
        new Service { Name = "Debug Log Viewer", ProjectName="DebugLogViewer", IsWPF=true, Port = 8003, StatusColor = Brushes.Gray },
        new Service { Name = "Visualizer", ProjectName="Visualizer", IsWPF=true, Port = 8004, StatusColor = Brushes.Gray },
        new Service { Name = "Designer", ProjectName="Designer", IsWPF=true, Port = 8005, StatusColor = Brushes.Gray }
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
        ServiceLauncher = new(isDebug);
        ServiceMonitor = new();

        // Register the converter in the resources
        this.Resources.Add("IndexToRowConverter", new IndexToRowConverter());
        InitializeComponent();
        DataContext = this;

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
        foreach (var service in Services)
        {
            ServiceLauncher.Kill(service);
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
}