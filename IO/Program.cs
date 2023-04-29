global using NothingButNeurons.Shared;
global using NothingButNeurons.Shared.Messages;
global using NothingButNeurons.CCSL;
global using System.Diagnostics;
using Proto;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace NothingButNeurons.IO
{
    internal class Program
    {
        static ActorSystem ProtoSystem;
        static PID HiveMind;

        static void Main(string[] args)
        {
            Debug.WriteLine("NothingButNeurons.IO program starting...");

            var remoteConfig = GrpcNetRemoteConfig
                .BindToLocalhost(8000)
                .WithProtoMessages(DebuggerReflection.Descriptor, NeuronsReflection.Descriptor, IOReflection.Descriptor)
                .WithRemoteDiagnostics(true);
            ProtoSystem = new ActorSystem().WithRemote(remoteConfig);
            ProtoSystem.Remote().StartAsync();

            HiveMind = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new HiveMind()), "HiveMind");
            //Debug.WriteLine($"\n\n\nHIVEMIND PID FROM IO: {HiveMind}\n\n\n");

            Console.ReadLine();
            ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
        }
    }
}