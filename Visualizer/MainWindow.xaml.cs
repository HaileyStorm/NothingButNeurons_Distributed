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

        PID? debugServerPID = await Nodes.GetPIDFromSettings(ProtoSystem.Root, "DebugServer");
        CCSL.Console.CombinedWriteLine($"Got DebugServer PID: {debugServerPID}");
        NetworkVisualizationUpdater = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new NetworkVisualization.Updater(debugServerPID, networkVisualizationCanvas, tickTime)), "NetworkVisualizationUpdater");
        Nodes.SendNodeOnline(ProtoSystem.Root, "Visualizer", NetworkVisualizationUpdater);
    }

    private void OnProcessExit(object sender, EventArgs e)
    {
        Nodes.SendNodeOffline(ProtoSystem.Root, "Visualizer");
    }
}
