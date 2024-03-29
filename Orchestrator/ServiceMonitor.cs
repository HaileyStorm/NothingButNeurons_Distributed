﻿using Proto;
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

    internal ServiceMonitor(PID? debugServerPID) : base(debugServerPID, "Orchestrator")
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
        // Process base class messages first
        bool processed = base.ReceiveMessage(context);
        // Do NOT return if processed like usual - we need to catch DebugServer PID changes, which are handled by NodeBase ReceiveMessage.

        switch (context.Message)
        {
            case SettingChangedMessage msg:
                CCSL.Console.CombinedWriteLine($"Received SettingChangedMessage. Table: {msg.TableName}, Setting: {msg.Setting}, Value: {msg.Value}");
                if (string.Equals(msg.TableName, "NodeStatus", StringComparison.InvariantCultureIgnoreCase))
                {
                    Service? service = Services.Where(s => string.Equals(s.ProjectName, msg.Setting, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault(defaultValue: null);
                    if (service != null)
                    {
                        if (string.IsNullOrEmpty(msg.Value))
                        {
                            service.PID = null;
                            if (service.StatusColor != Brushes.Gray && service.StatusColor != Brushes.Red)
                                service.StatusColor = Brushes.Red;
                        } else
                        {
                            service.PID = msg.Value;
                            if (service.StatusColor != Brushes.Green)
                            {
                                service.StatusColor = Brushes.Green;
                                // Preventing launching another copy (not that this is *always* a problem, but it's unnecessary for sure, at least for now)
                                service.Enabled = false;
                            }
                        }
                    }
                    processed = true;
                }
                break;
        }

        return processed;
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
            //PID servicePID = PID.FromAddress($"127.0.0.1:{service.Port}", service.ActorName);
            if (string.IsNullOrEmpty(service.PID))
            {
                CCSL.Console.CombinedWriteLine($"Non-gray/red service PID unexpectedly null, for service: {service.Name}. Cannot ping. Setting service offline.");
                service.StatusColor = Brushes.Red;
                Nodes.SendNodeOffline(BaseContext!, service.ProjectName);
                continue;
            }
            BaseContext!.RequestAsync<PongMessage>(Nodes.GetPIDFromString(service.PID)!, new PingMessage { }, new System.Threading.CancellationToken()).WaitUpTo(TimeSpan.FromMilliseconds(850)).ContinueWith(x =>
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
                            Nodes.SendNodeOffline(BaseContext!, service.ProjectName);
                            service.PID = null;
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