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

        ProtoSystem = Nodes.GetActorSystem(Port);

        DebugServer = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new DebugServer()), "DebugServer");
        Nodes.SendNodeOnline(ProtoSystem.Root, "DebugServer", DebugServer);

        CCSL.Console.CombinedWriteLine("NothingButNeurons.DebugServer program ready.");

        System.Console.ReadLine();
        CCSL.Console.CombinedWriteLine("NothingButNeurons.DebugServer program shutting down...");
        Nodes.SendNodeOffline(ProtoSystem.Root, "DebugServer");
        ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
    }

    
}