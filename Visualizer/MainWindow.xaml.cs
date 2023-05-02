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
    static int DebugServerPort;

    PID NetworkVisualizationUpdater;

    public MainWindow()
    {
        InitializeComponent();

        InitializeActorSystem();
    }

    /// <summary>
    /// Initializes the Proto.Actor system.
    /// </summary>
    private void InitializeActorSystem()
    {
        int tickTime = 300;

        // Get command-line arguments
        string[] args = Environment.GetCommandLineArgs();
        if (args.Length >= 2)
        {
            // In this app, the first argument is a dll
            args = args[1].Split(' ');
            Port = int.Parse(args[0]);
            DebugServerPort = int.Parse(args[1]);
        }
        else
        {
            Port = Shared.Consts.DefaultPorts.VISUALIZER;
            DebugServerPort = Shared.Consts.DefaultPorts.DEBUG_SERVER;
        }

        ProtoSystem = Nodes.GetActorSystem(Port);

        PID debugServerPID = PID.FromAddress($"127.0.0.1:{DebugServerPort}", "DebugServer");
        NetworkVisualizationUpdater = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new NetworkVisualization.Updater(debugServerPID, networkVisualizationCanvas, tickTime)), "NetworkVisualizationUpdater");
    }
}
