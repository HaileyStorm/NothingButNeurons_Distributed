using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace NothingButNeurons.Orchestrator;

internal class ServiceMonitor
{
    private ObservableCollection<Service> Services;
    private Timer _timer;

    internal ServiceMonitor()
    {
        Services = MainWindow.Instance.Services;

        _timer = new Timer(1000);
        _timer.Elapsed += Monitor;
        _timer.Start();
    }

    private void Monitor(object sender, ElapsedEventArgs e)
    {
        //TODO: Ping...
    }
}
