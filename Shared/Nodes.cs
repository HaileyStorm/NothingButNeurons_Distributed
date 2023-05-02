using NothingButNeurons.Shared.Messages;
using Proto.Remote.GrpcNet;
using Proto;
using Proto.Remote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NothingButNeurons.Shared;

public static class Nodes
{
    public static GrpcNetRemoteConfig GetRemoteConfig(int port)
    {
        return GrpcNetRemoteConfig
            .BindToLocalhost(port)
            .WithProtoMessages(DebuggerReflection.Descriptor, NeuronsReflection.Descriptor, IOReflection.Descriptor)
            /*.WithChannelOptions(new GrpcChannelOptions
            {
                CompressionProviders = new[]
                        {
                            new GzipCompressionProvider(CompressionLevel.Fastest)
                        }
            })*/
            .WithRemoteDiagnostics(true);
    }

    public static ActorSystem GetActorSystem(GrpcNetRemoteConfig remoteConfig)
    {
        ActorSystem actorSystem = new ActorSystem().WithRemote(remoteConfig);
        actorSystem.Remote().StartAsync();
        while (!actorSystem.Remote().Started)
        {
            Thread.Sleep(100);
        }
        return actorSystem;
    }
    public static ActorSystem GetActorSystem(int port)
    {
        return GetActorSystem(GetRemoteConfig(port));
    }

    /*I don't like the way this worked out, returning an array ... doesn't end up simplifying much
     * public static int[] GetCommandLinePorts(int numPorts, int[]? defaultPorts = null, bool hasDLL = false)
    {
        return GetCommandLinePorts(Environment.GetCommandLineArgs(), numPorts, defaultPorts, hasDLL);
    }
    public static int[] GetCommandLinePorts(string[] args, int numPorts, int[]? defaultPorts = null, bool hasDLL = false)
    {
        defaultPorts ??= Array.Empty<int>();
        int minArgs = hasDLL ? 2 : numPorts;

        if (args.Length < minArgs)
            return defaultPorts;
        if (hasDLL)
        {
            args = args[1].Split(' ');
            if (args.Length < numPorts)
                return defaultPorts;
        }

        int[] ports = new int[numPorts];
        for (int i = 0; i < numPorts; i++)
        {
            ports[i] = int.Parse(args[i]);
        }

        return ports;
    }*/
}