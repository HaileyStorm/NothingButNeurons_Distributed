using NothingButNeurons.Shared.Messages;
using Proto.Remote.GrpcNet;
using Proto;
using Proto.Remote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NothingButNeurons.Shared.Consts;

namespace NothingButNeurons.Shared;

public static class Nodes
{
    public static GrpcNetRemoteConfig GetRemoteConfig(int port)
    {
        return GrpcNetRemoteConfig
            .BindToLocalhost(port)
            .WithProtoMessages(Meta.FileDescriptors)
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

    public static async Task<int?> GetPortFromSettings(IContext context, string projectName, PID? settingsPID = null)
    {
        return await GetPortFromSettingsInternal(context, projectName, settingsPID);
    }
    public static async Task<int?> GetPortFromSettings(IRootContext context, string projectName, PID? settingsPID = null)
    {
        return await GetPortFromSettingsInternal(context, projectName, settingsPID);
    }
    private static async Task<int?> GetPortFromSettingsInternal(object context, string projectName, PID? settingsPID = null)
    {
        settingsPID ??= PID.FromAddress($"127.0.0.1:{DefaultPorts.SETTINGS_MONITOR}", "SettingsMonitor");

        int? port = null;

        // Define a local function to handle the response and avoid duplicate code
        void HandleResponse(Task<SettingResponseMessage> x)
        {
            if (x.IsFaulted)
                throw x.Exception!;
            else
                port = int.Parse(x.Result.Value);
        }

        if (context is IContext icontext)
        {
            await icontext.RequestAsync<SettingResponseMessage>(settingsPID, new SettingRequestMessage { TableName = "Ports", Setting = projectName }).ContinueWith(HandleResponse);
        }
        else if (context is IRootContext rcontext)
        {
            await rcontext.RequestAsync<SettingResponseMessage>(settingsPID, new SettingRequestMessage { TableName = "Ports", Setting = projectName }).ContinueWith(HandleResponse);
        }

        return port;
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