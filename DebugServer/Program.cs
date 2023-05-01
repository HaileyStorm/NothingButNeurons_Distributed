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
            Port = 8001;
        }

        CombinedWriteLine($"NothingButNeurons.DebugServer program starting on port {Port}...");

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

        DebugServer = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new DebugServer()), "DebugServer");

        CombinedWriteLine("NothingButNeurons.DebugServer program ready.");

        Console.ReadLine();
        CombinedWriteLine("NothingButNeurons.DebugServer program shutting down...");
        ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
    }

    static void CombinedWriteLine(string line)
    {
        Debug.WriteLine(line);
        Console.WriteLine(line);
    }
}