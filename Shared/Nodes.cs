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
using Proto.Utils;

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
            await icontext.RequestAsync<SettingResponseMessage>(settingsPID, new SettingRequestMessage { TableName = "Ports", Setting = projectName }).ContinueWith(HandleResponse);
        else if (context is IRootContext rcontext)
            await rcontext.RequestAsync<SettingResponseMessage>(settingsPID, new SettingRequestMessage { TableName = "Ports", Setting = projectName }).ContinueWith(HandleResponse);

        return port;
    }

    public static void SendNodeOnline(IContext context, string projectName, PID projectPID, PID? settingsPID = null)
    {
        SendNodeOnlineInternal(context, projectName, projectPID, settingsPID);
    }
    public static void SendNodeOnline(IRootContext context, string projectName, PID projectPID, PID? settingsPID = null)
    {
        SendNodeOnlineInternal(context, projectName, projectPID, settingsPID);
    }
    private static void SendNodeOnlineInternal(object context, string projectName, PID projectPID, PID? settingsPID = null)
    {
        settingsPID ??= PID.FromAddress($"127.0.0.1:{DefaultPorts.SETTINGS_MONITOR}", "SettingsMonitor");

        NodeOnlineMessage msg = new NodeOnlineMessage() { Name = projectName, PID = projectPID };

        if (context is IContext icontext)
            icontext.Send(settingsPID, msg);
        else if (context is IRootContext rcontext)
            rcontext.Send(settingsPID, msg);
    }

    public static void SendNodeOffline(IContext context, string projectName, PID? settingsPID = null)
    {
        SendNodeOfflineInternal(context, projectName, settingsPID);
    }
    public static void SendNodeOffline(IRootContext context, string projectName, PID? settingsPID = null)
    {
        SendNodeOfflineInternal(context, projectName, settingsPID);
    }
    private static void SendNodeOfflineInternal(object context, string projectName, PID? settingsPID = null)
    {
        settingsPID ??= PID.FromAddress($"127.0.0.1:{DefaultPorts.SETTINGS_MONITOR}", "SettingsMonitor");

        NodeOnlineMessage msg = new NodeOnlineMessage() { Name = projectName, PID = null };

        if (context is IContext icontext)
            icontext.Send(settingsPID, msg);
        else if (context is IRootContext rcontext)
            rcontext.Send(settingsPID, msg);
    }

    public static async Task<PID?> GetPIDFromSettings(IContext context, string projectName, PID? settingsPID = null)
    {
        return await GetPIDFromSettingsInternal(context, projectName, settingsPID);
    }
    public static async Task<PID?> GetPIDFromSettings(IRootContext context, string projectName, PID? settingsPID = null)
    {
        return await GetPIDFromSettingsInternal(context, projectName, settingsPID);
    }
    private static async Task<PID?> GetPIDFromSettingsInternal(object context, string projectName, PID? settingsPID = null)
    {
        settingsPID ??= PID.FromAddress($"127.0.0.1:{DefaultPorts.SETTINGS_MONITOR}", "SettingsMonitor");

        PID? pid = null;

        void HandleResponse(Task<SettingResponseMessage> x)
        {
            if (x.IsFaulted)
                throw x.Exception!;
            else
                pid = GetPIDFromString(x.Result.Value);
        }

        if (context is IContext icontext)
            await icontext.RequestAsync<SettingResponseMessage>(settingsPID, new SettingRequestMessage { TableName = "NodeStatus", Setting = projectName }).ContinueWith(HandleResponse);
        else if (context is IRootContext rcontext)
            await rcontext.RequestAsync<SettingResponseMessage>(settingsPID, new SettingRequestMessage { TableName = "NodeStatus", Setting = projectName }).ContinueWith(HandleResponse);

        return pid;
    }

    public static PID? GetPIDFromString(string? pidString)
    {
        PID? pid = null;

        if (!string.IsNullOrEmpty(pidString))
        {
            int slashIndex = pidString.IndexOf('/');
            if (slashIndex != -1)
            {
                string address = pidString.Substring(0, slashIndex);
                string id = pidString.Substring(slashIndex + 1);
                pid = new PID(address, id);
            }
        }

        return pid;
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