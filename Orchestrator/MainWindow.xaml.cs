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
}

public class Service
{
    public string Name { get; set; }
    public string ProjectName { get; set; }
    public bool IsWPF { get; set; }
    public int Port { get; set; }
    public SolidColorBrush StatusColor { get; set; }
}
