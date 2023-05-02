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
    static int DebugServerPort;
    static PID DebugFileWriter;

    static void Main(string[] args)
    {
        if (args.Length >= 2)
        {
            Port = int.Parse(args[0]);
            DebugServerPort = int.Parse(args[1]);
        }
        else
        {
            Port = Shared.Consts.DefaultPorts.DEBUG_FILE_WRITER;
            DebugServerPort = Shared.Consts.DefaultPorts.DEBUG_SERVER;
        }

        CombinedWriteLine($"NothingButNeurons.DebugFileWriter program starting on port {Port}, with DebugServer on port {DebugServerPort}...");

        ProtoSystem = Nodes.GetActorSystem(Port);

        PID debugServerPID = PID.FromAddress($"127.0.0.1:{DebugServerPort}", "DebugServer");
        DebugFileWriter = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new DebugFileWriter(debugServerPID)), "DebugFileWriter");

        CombinedWriteLine("NothingButNeurons.DebugFileWriter program ready.");

        Console.ReadLine();
        CombinedWriteLine("NothingButNeurons.DebugFileWriter program shutting down...");
        ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
    }

    static void CombinedWriteLine(string line)
    {
        Debug.WriteLine(line);
        Console.WriteLine(line);
    }
}