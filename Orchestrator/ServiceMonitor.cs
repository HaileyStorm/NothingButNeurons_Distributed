using Proto;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using Proto.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media;

namespace NothingButNeurons.Orchestrator;

internal class ServiceMonitor
{
    private ObservableCollection<Service> Services;
    private System.Timers.Timer _timer;
    private ActorSystem ProtoSystem;

    internal ServiceMonitor()
    {
        Services = MainWindow.Instance.Services;

        ProtoSystem = Nodes.GetActorSystem(MainWindow.ServiceMonitorPort);

        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += Monitor;
        _timer.Start();
    }

    private void Monitor(object sender, ElapsedEventArgs e)
    {
        foreach (var service in Services.Where(s => s.StatusColor != Brushes.Red && s.StatusColor != Brushes.Gray))
        {
            PID servicePID = PID.FromAddress($"127.0.0.1:{service.Port}", service.ActorName);
            ProtoSystem.Root.RequestAsync<PongMessage>(servicePID, new PingMessage { }, new CancellationToken()).WaitUpTo(TimeSpan.FromMilliseconds(850)).ContinueWith(x =>
            {
                if (x.IsFaulted || !x.Result.completed)
                {
                    switch (service.StatusColor)
                    {
                        case SolidColorBrush b when b == Brushes.Green:
                            service.StatusColor = Brushes.Yellow;
                            break;
                        case SolidColorBrush b when b == Brushes.Yellow:
                            service.StatusColor = Brushes.Orange;
                            break;
                        case SolidColorBrush b when b == Brushes.Orange:
                            service.StatusColor = Brushes.Red;
                            break;
                    }
                } else
                {
                    if (service.StatusColor != Brushes.Green)
                        service.StatusColor = Brushes.Green;
                }
            });
        }
    }
}