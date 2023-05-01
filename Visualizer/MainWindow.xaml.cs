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
            Port = int.Parse(args[0]);
            DebugServerPort = int.Parse(args[1]);
        }
        else
        {
            Port = 8004;
            DebugServerPort = 8001;
        }

        var remoteConfig = GrpcNetRemoteConfig
                .BindToLocalhost(Port)
                .WithProtoMessages(DebuggerReflection.Descriptor, NeuronsReflection.Descriptor, IOReflection.Descriptor)
                /*.WithChannelOptions(new GrpcChannelOptions
                    {
                        CompressionProviders = new[]
                        {
                            new GzipCompressionProvider(CompressionLevel.Fastest)
                        }
                    })*/
                .WithRemoteDiagnostics(true);
        ProtoSystem = new ActorSystem().WithRemote(remoteConfig);
        ProtoSystem.Remote().StartAsync();
        while (!ProtoSystem.Remote().Started)
        {
            Thread.Sleep(100);
        }

        PID debugServerPID = PID.FromAddress($"127.0.0.1:{DebugServerPort}", "DebugServer");
        NetworkVisualizationUpdater = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new NetworkVisualization.Updater(debugServerPID, networkVisualizationCanvas, tickTime)), "NetworkVisualizationUpdater");
    }
}
