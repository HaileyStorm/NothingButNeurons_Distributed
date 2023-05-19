global using NothingButNeurons.Shared;
global using NothingButNeurons.Shared.Messages;
global using NothingButNeurons.CCSL;
global using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Proto;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using Google.Protobuf;
using System.Threading;

namespace NothingButNeurons.Visualizer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    static ActorSystem ProtoSystem;
    static int Port;

    PID NetworkVisualizationUpdater;
    PID? DebugServerPID;

    public MainWindow()
    {
        InitializeComponent();

        InitializeActorSystem();

        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    /// <summary>
    /// Initializes the Proto.Actor system.
    /// </summary>
    private async void InitializeActorSystem()
    {
        int tickTime = 300;

        // Get command-line arguments
        string[] args = Environment.GetCommandLineArgs();
        if (args.Length >= 2)
        {
            // In this app, the first argument is a dll
            args = args[1].Split(' ');
            Port = int.Parse(args[0]);
        }
        else
        {
            Port = Shared.Consts.DefaultPorts.VISUALIZER;
        }

        ProtoSystem = Nodes.GetActorSystem(Port);

        DebugServerPID = await Nodes.GetPIDFromSettings(ProtoSystem.Root, "DebugServer");
        CCSL.Console.CombinedWriteLine($"Got DebugServer PID: {DebugServerPID}");
        NetworkVisualizationUpdater = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new NetworkVisualization.Updater(DebugServerPID, networkVisualizationCanvas, tickTime)), "NetworkVisualizationUpdater");

        ProtoSystem.EventStream.Subscribe((SelfPortChangedMessage msg) => {
            CCSL.Console.CombinedWriteLine($"Visualizer got SelfPortChangedMessage with new port: {msg.Port}. THIS NODE CANNOT BE RESTARTED AND WILL CONTINUE RUNNING ON ITS OLD PORT.");
            if (DebugServerPID != null)
            {
                ProtoSystem.Root.Send(DebugServerPID, new DebugOutboundMessage()
                {
                    Severity = DebugSeverity.Error,
                    Context = "Node",
                    Summary = "Visualizer Node received SelfPortChangedMessage event",
                    Message = "Visualizer cannot be restarted (will have missed spawn debugs) and will continue running on its old port.",
                    MessageSentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                });
            }

            // Not a good idea for Visualizer Node:
            //Port = msg.Port;
            //_InitializeActorSystem();
        });

        Nodes.SendNodeOnline(ProtoSystem.Root, "Visualizer", NetworkVisualizationUpdater);
    }

    private void OnProcessExit(object sender, EventArgs e)
    {
        Nodes.SendNodeOffline(ProtoSystem.Root, "Visualizer");
        ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
    }
}
