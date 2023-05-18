global using NothingButNeurons.Shared;
global using NothingButNeurons.Shared.Messages;
global using NothingButNeurons.CCSL;
global using System.Diagnostics;
using Proto;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using Grpc.Net.Client;
using System.IO.Compression;
using Grpc.Net.Compression;

namespace NothingButNeurons.IO
{
    internal class Program
    {
        static ActorSystem ProtoSystem;
        static int Port;
        static int? DebugServerPort;
        static PID HiveMind;

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Port = int.Parse(args[0]);
            } else
            {
                Port = Shared.Consts.DefaultPorts.IO;
            }

            CCSL.Console.CombinedWriteLine($"NothingButNeurons.IO program starting on port {Port}...");

            InitializeActorSystem();

            System.Console.ReadLine();
            CCSL.Console.CombinedWriteLine("NothingButNeurons.IO program shutting down...");
            Nodes.SendNodeOffline(ProtoSystem.Root, "IO");
            ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
        }

        private static async void InitializeActorSystem()
        {
            ProtoSystem = Nodes.GetActorSystem(Port);

            PID? debugServerPID = await Nodes.GetPIDFromSettings(ProtoSystem.Root, "DebugServer");
            CCSL.Console.CombinedWriteLine($"Got DebugServer PID: {debugServerPID}");

            HiveMind = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new HiveMind(debugServerPID)), "HiveMind");
            Nodes.SendNodeOnline(ProtoSystem.Root, "IO", HiveMind);

            CCSL.Console.CombinedWriteLine("NothingButNeurons.IO program ready.");
        }
    }
}