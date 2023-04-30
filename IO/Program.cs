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
        static PID HiveMind;

        static void Main(string[] args)
        {
            CombinedWriteLine("NothingButNeurons.IO program starting...");

            var remoteConfig = GrpcNetRemoteConfig
                .BindToLocalhost(8000)
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

            PID debugServerPID = PID.FromAddress("127.0.0.1:8001", "DebugServer");
            HiveMind = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new HiveMind(debugServerPID)), "HiveMind");

            CombinedWriteLine("NothingButNeurons.IO program ready.");

            Console.ReadLine();
            CombinedWriteLine("NothingButNeurons.IO program shutting down...");
            ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
        }

        static void CombinedWriteLine(string line)
        {
            Debug.WriteLine(line);
            Console.WriteLine(line);
        }
    }
}