global using NothingButNeurons.Shared;
global using NothingButNeurons.Shared.Messages;
global using NothingButNeurons.CCSL;
global using System.Diagnostics;
using Proto;
using Proto.Remote.GrpcNet;
using Proto.Remote;
using System.Security.Cryptography;

namespace NothingButNeurons.DebugFileWriter;

internal class Program
{
    static ActorSystem ProtoSystem;
    static int Port;
    static PID DebugFileWriter;

    static void Main(string[] args)
    {
        if (args.Length >= 1)
        {
            Port = int.Parse(args[0]);
        }
        else
        {
            Port = Shared.Consts.DefaultPorts.DEBUG_FILE_WRITER;
        }

        CCSL.Console.CombinedWriteLine($"NothingButNeurons.DebugFileWriter program starting on port {Port}...");

        InitializeActorSystem();

        AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);

        System.Console.ReadLine();
        OnProcessExit(ProtoSystem, new EventArgs());
    }

    private static async void InitializeActorSystem()
    {
        _InitializeActorSystem();

        ProtoSystem.EventStream.Subscribe(async (SelfPortChangedMessage msg) => {
            CCSL.Console.CombinedWriteLine($"DebugFileWriter got SelfPortChangedMessage with new port: {msg.Port}. Restarting ActorSystem.");
            Port = msg.Port;
            await ProtoSystem.Remote().ShutdownAsync();
            Thread.Sleep(5000);
            _InitializeActorSystem();
        });

        CCSL.Console.CombinedWriteLine("NothingButNeurons.DebugFileWriter program ready.");
    }

    private static async void _InitializeActorSystem()
    {
        ProtoSystem = Nodes.GetActorSystem(Port);

        PID? debugServerPID = await Nodes.GetPIDFromSettings(ProtoSystem.Root, "DebugServer");
        DebugFileWriter = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new DebugFileWriter(debugServerPID)), "DebugFileWriter");
        Nodes.SendNodeOnline(ProtoSystem.Root, "DebugFileWriter", DebugFileWriter);
    }

    private static void OnProcessExit(object sender, EventArgs e)
    {
        CCSL.Console.CombinedWriteLine("NothingButNeurons.DebugFileWriter program shutting down...");
        Nodes.SendNodeOffline(ProtoSystem.Root, "DebugFileWriter");
        ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
    }
}