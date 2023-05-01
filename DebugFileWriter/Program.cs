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
            Port = 8002;
            DebugServerPort = 8001;
        }

        CombinedWriteLine($"NothingButNeurons.DebugFileWriter program starting on port {Port}, with DebugServer on port {DebugServerPort}...");

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