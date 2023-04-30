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
        static int DebugServerPort;
        static PID HiveMind;

        static void Main(string[] args)
        {
            if (args.Length >= 2)
            {
                Port = int.Parse(args[0]);
                DebugServerPort = int.Parse(args[1]);
            } else
            {
                Port = 8000;
                DebugServerPort = 8001;
            }
            
            CombinedWriteLine($"NothingButNeurons.IO program starting on port {Port}, with DebugServer on port {DebugServerPort}...");

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