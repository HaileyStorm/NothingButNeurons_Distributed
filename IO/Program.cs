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
            Debug.WriteLine("NothingButNeurons.IO program starting...");
            Console.WriteLine("NothingButNeurons.IO program starting...");

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
            //Debug.WriteLine($"\n\n\nDEBUG SERVER PID FROM IO: {debugServerPID}\n\n\n");
            HiveMind = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new HiveMind(debugServerPID)), "HiveMind");
            //Debug.WriteLine($"\n\n\nHIVEMIND PID FROM IO: {HiveMind}\n\n\n");

            Debug.WriteLine("NothingButNeurons.IO program ready.");
            Console.WriteLine("NothingButNeurons.IO program ready.");

            Console.ReadLine();
            Debug.WriteLine("NothingButNeurons.IO program shutting down...");
            Console.WriteLine("NothingButNeurons.IO program shutting down...");
            ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
        }
    }
}