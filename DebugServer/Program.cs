global using NothingButNeurons.Shared;
global using NothingButNeurons.Shared.Messages;
global using NothingButNeurons.CCSL;
global using System.Diagnostics;
using Proto;
using Proto.Remote.GrpcNet;
using Proto.Remote;

namespace NothingButNeurons.DebugServer;

internal class Program
{
    static ActorSystem ProtoSystem;
    static int Port;
    static PID DebugServer;

    static void Main(string[] args)
    {
        if (args.Length >= 1)
        {
            Port = int.Parse(args[0]);
        }
        else
        {
            Port = Shared.Consts.DefaultPorts.DEBUG_SERVER;
        }

        CCSL.Console.CombinedWriteLine($"NothingButNeurons.DebugServer program starting on port {Port}...");

        InitializeActorSystem();

        AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);

        System.Console.ReadLine();
        OnProcessExit(ProtoSystem, new EventArgs());
    }

    private static async void InitializeActorSystem()
    {
        _InitializeActorSystem();

        ProtoSystem.EventStream.Subscribe(async (SelfPortChangedMessage msg) => {
            CCSL.Console.CombinedWriteLine($"DebugServer got SelfPortChangedMessage with new port: {msg.Port}. Restarting ActorSystem.");
            
            Port = msg.Port;
            await ProtoSystem.Remote().ShutdownAsync();
            Thread.Sleep(5000);
            _InitializeActorSystem();
        });

        CCSL.Console.CombinedWriteLine("NothingButNeurons.DebugServer program ready.");
    }

    private static async void _InitializeActorSystem()
    {
        ProtoSystem = Nodes.GetActorSystem(Port);

        DebugServer = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new DebugServer()), "DebugServer");
        Nodes.SendNodeOnline(ProtoSystem.Root, "DebugServer", DebugServer);
    }

    private static void OnProcessExit(object sender, EventArgs e)
    {
        CCSL.Console.CombinedWriteLine("NothingButNeurons.DebugServer program shutting down...");
        Nodes.SendNodeOffline(ProtoSystem.Root, "DebugServer");
        ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
    }
}