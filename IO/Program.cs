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
        static PID? DebugServerPID;
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

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);

            System.Console.ReadLine();
            OnProcessExit(ProtoSystem, new EventArgs());
        }

        private static async void InitializeActorSystem()
        {
            _InitializeActorSystem();

            ProtoSystem.EventStream.Subscribe((SelfPortChangedMessage msg) => {
                CCSL.Console.CombinedWriteLine($"IO got SelfPortChangedMessage with new port: {msg.Port}. THIS NODE CANNOT BE RESTARTED AND WILL CONTINUE RUNNING ON ITS OLD PORT.");
                if (DebugServerPID != null)
                {
                    ProtoSystem.Root.Send(DebugServerPID, new DebugOutboundMessage()
                    {
                        Severity = DebugSeverity.Critical,
                        Context = "Node",
                        Summary = "IO Node received SelfPortChangedMessage event",
                        Message = "IO cannot be restarted and will continue running on its old port.",
                        MessageSentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                    });
                }

                // Not a good idea for IO Node:
                //Port = msg.Port;
                //ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
                //_InitializeActorSystem();
            });

            CCSL.Console.CombinedWriteLine("NothingButNeurons.IO program ready.");
        }

        private static async void _InitializeActorSystem()
        {
            ProtoSystem = Nodes.GetActorSystem(Port);

            DebugServerPID = await Nodes.GetPIDFromSettings(ProtoSystem.Root, "DebugServer");

            HiveMind = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new HiveMind(DebugServerPID)), "HiveMind");
            Nodes.SendNodeOnline(ProtoSystem.Root, "IO", HiveMind);
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            CCSL.Console.CombinedWriteLine("NothingButNeurons.IO program shutting down...");
            Nodes.SendNodeOffline(ProtoSystem.Root, "IO");
            ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
        }
    }
}