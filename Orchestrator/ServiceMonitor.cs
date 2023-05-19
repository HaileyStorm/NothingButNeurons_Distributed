using Proto;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using Proto.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media;

namespace NothingButNeurons.Orchestrator;

internal class ServiceMonitor : NodeBase
{
    private ObservableCollection<Service> Services;
    private System.Timers.Timer _timer;

    internal ServiceMonitor(PID? debugServerPID) : base(debugServerPID)
    {
        Services = MainWindow.Instance.Services;

        // Initial enabled status
        foreach (var service in Services)
        {
            service.Enabled = !service.PreReqProjects.Any();
        }
    }

    protected override bool ReceiveMessage(IContext context)
    {
        switch (context.Message)
        {
            case SettingChangedMessage msg:
                CCSL.Console.CombinedWriteLine($"Received SettingChangedMessage. Table: {msg.TableName}, Setting: {msg.Setting}, Value: {msg.Value}");
                break;
        }
        return true;
    }

    protected override void ProcessRestartingMessage(IContext context, Restarting msg)
    {
    }

    protected override void ProcessStartedMessage(IContext context, Started msg)
    {
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += Monitor;
        _timer.Start();
    }

    protected override void ProcessStoppingMessage(IContext context, Stopping msg)
    {
    }

    protected override void ProcessStoppedMessage(IContext context, Stopped msg)
    {
    }

    private void Monitor(object sender, ElapsedEventArgs e)
    {
        // Ping running services and update status according to response
        foreach (var service in Services.Where(s => s.StatusColor != Brushes.Red && s.StatusColor != Brushes.Gray))
        {
            PID servicePID = PID.FromAddress($"127.0.0.1:{service.Port}", service.ActorName);
            BaseContext!.RequestAsync<PongMessage>(servicePID, new PingMessage { }, new System.Threading.CancellationToken()).WaitUpTo(TimeSpan.FromMilliseconds(850)).ContinueWith(x =>
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
                    {
                        service.StatusColor = Brushes.Green;
                        // Preventing launching another copy (not that this is *always* a problem, but it's unnecessary for sure, at least for now)
                        service.Enabled = false;
                    }
                }
            });
        }

        // Enable or disable services based on whether prepreqs are running (green - already running - are disabled and don't need to be reenabled)
        foreach (var service in Services.Where(s => s.StatusColor != Brushes.Green))
        {
            bool enable = true;
            foreach (var project in service.PreReqProjects)
            {
                // Found a prereq that isn't running
                if (Services.Where(s => string.Equals(s.ProjectName, project, StringComparison.InvariantCultureIgnoreCase)).First().StatusColor != Brushes.Green)
                {
                    enable = false;
                    break;
                }
            }
            service.Enabled = enable;
        }
    }
}