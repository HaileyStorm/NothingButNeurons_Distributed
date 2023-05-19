global using NothingButNeurons.Shared;
global using NothingButNeurons.Shared.Messages;
global using NothingButNeurons.CCSL;
global using System.Diagnostics;
using Proto;
using Proto.Remote.GrpcNet;
using Proto.Remote;
using MySqlConnector;
using System.Data;
using NothingButNeurons.Shared.Consts;
using Proto.Utils;

namespace NothingButNeurons.SettingsMonitor;

internal class Program
{
    const string ConnectionString = "Server=35.184.181.185;User ID=root;Password=m3)YvNVlZH)4%_A.;Database=settings";
    const int QueryInterval = 5; // Seconds

    static int Port;
    static ActorSystem ProtoSystem;
    static PID Monitor;
    static PID? DebugServerPID;
    static MySqlConnection Connection;
    static System.Timers.Timer QueryTimer;
    static DateTime LastQueryTime;

    static void Main(string[] args)
    {
        Port = DefaultPorts.SETTINGS_MONITOR;

        CCSL.Console.CombinedWriteLine($"NothingButNeurons.SettingsMonitor program starting on port {Port}...");

        ProtoSystem = Nodes.GetActorSystem(Port);

        InitializeDbConnection();

        InitializeActorSystem();

        AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);

        System.Console.ReadLine();
        OnProcessExit(ProtoSystem, new EventArgs());
    }

    static void InitializeDbConnection()
    {
        Connection = new MySqlConnection(ConnectionString);
        Connection.Open();
    }

    static async void InitializeActorSystem()
    {
        // Get the debugServerPID, and check/update the status of all nodes.
        string query = "SELECT Node, PID FROM NodeStatus;";
        using var cmd = new MySqlCommand(query, Connection);
        using var reader = await cmd.ExecuteReaderAsync();

        var nodeStatuses = new List<(string NodeName, string? PIDString)>();

        while (await reader.ReadAsync())
        {
            string nodeName = reader.GetString(0);
            string? pidString = reader.IsDBNull(1) ? null : reader.GetString(1);
            nodeStatuses.Add((nodeName, pidString));
        }

        reader.Close();

        DebugServerPID = null;

        foreach (var nodeStatus in nodeStatuses)
        {
            string nodeName = nodeStatus.NodeName;
            string? pidString = nodeStatus.PIDString;
            PID? currentPID = Nodes.GetPIDFromString(pidString);

            if (currentPID != null)
            {
                CCSL.Console.CombinedWriteLine($"{nodeName} reports it is online (NodeStatus db entry exists with non-null PID), checking if it actually is...");
                await ProtoSystem.Root.RequestAsync<PongMessage>(currentPID, new PingMessage { }, new System.Threading.CancellationToken()).WaitUpTo(TimeSpan.FromMilliseconds(850)).ContinueWith(async x =>
                {
                    if (x.IsFaulted || !x.Result.completed)
                    {
                        CCSL.Console.CombinedWriteLine($"{nodeName} isn't. Setting NodeStatus offline.");
                        await SettingsMonitor.UpdateNodeStatus(Connection, nodeName, null);
                        if (nodeName == "DebugServer")
                        {
                            DebugServerPID = null;
                        }
                    }
                    else
                    {
                        CCSL.Console.CombinedWriteLine($"{nodeName} is online.");
                        if (nodeName == "DebugServer")
                        {
                            DebugServerPID = currentPID;
                        }
                    }
                });
            }
        }

        Monitor = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new SettingsMonitor(ConnectionString, DebugServerPID)), "SettingsMonitor");

        ProtoSystem.EventStream.Subscribe((SelfPortChangedMessage msg) => {
            CCSL.Console.CombinedWriteLine($"SettingsMonitor got SelfPortChangedMessage with new port: {msg.Port}. THIS NODE CANNOT BE RESTARTED AND WILL CONTINUE RUNNING ON ITS OLD PORT.");
            if (DebugServerPID != null)
            {
                ProtoSystem.Root.Send(DebugServerPID, new DebugOutboundMessage()
                {
                    Severity = DebugSeverity.Critical,
                    Context = "Node",
                    Summary = "SettingMonitor Node received SelfPortChangedMessage event",
                    Message = "SettingMonitor cannot be restarted and will continue running on its old port.",
                    MessageSentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                });
            }

            // Not a good idea for SettingsMonitor Node:
            //Port = msg.Port;
            //_InitializeActorSystem();
        });

        CCSL.Console.CombinedWriteLine($"NothingButNeurons.SettingsMonitor program ready ({Monitor}).");

        LastQueryTime = DateTime.UtcNow.AddSeconds(-5 * QueryInterval);
        QueryTimer = new System.Timers.Timer(TimeSpan.FromSeconds(QueryInterval));
        QueryTimer.Elapsed += async (s, e) => {
            var newTime = DateTime.UtcNow;
            await GetChangesAfterAsync(LastQueryTime);
            LastQueryTime = newTime;
        };
        // Run it once now
        await GetChangesAfterAsync(LastQueryTime);
        LastQueryTime = DateTime.UtcNow;
        // And start the schedule
        QueryTimer.Start();
    }

    public static async Task GetChangesAfterAsync(DateTime dateTimeUtc)
    {
        // This query uses a subquery to find the maximum changeTimestamp for each combination of tableName and setting.
        // The subquery returns only the most recent changeTimestamp for each combination.
        // The outer query then joins the original table with the subquery results to only return the rows with the most recent changeTimestamp for each combination.
        string query = "SELECT t1.tableName, t1.setting, t1.value\nFROM SettingsChangeLog t1\nINNER JOIN (\n    SELECT tableName, setting, MAX(changeTimestamp) AS maxChangeTimestamp\n    FROM SettingsChangeLog\n    WHERE changeTimestamp > @dateTimeUtc\n    GROUP BY tableName, setting\n) t2\nON t1.tableName = t2.tableName AND t1.setting = t2.setting AND t1.changeTimestamp = t2.maxChangeTimestamp";
        using MySqlCommand command = new(query, Connection);
        command.Parameters.AddWithValue("@dateTimeUtc", dateTimeUtc);

        using MySqlDataReader reader = await command.ExecuteReaderAsync();

        while (reader.Read())
        {
            string tableName = reader.GetString(0);
            string setting = reader.GetString(1);
            object val = reader.GetValue(2);
            string value = "";
            if (val is not DBNull)
                value = (string)val;

            HandleSettingsEntry(tableName, setting, value);
        }
    }

    private static void HandleSettingsEntry(string tableName, string setting, string value)
    {
        CCSL.Console.CombinedWriteLine($"Setting change detected: Table: {tableName}, Setting: {setting}, Value: {value}");

        ProtoSystem.EventStream.Publish(new SettingChangedMessage() { TableName=tableName, Setting=setting, Value=value });
    }
    private static async void OnProcessExit(object sender, EventArgs e)
    {
        CCSL.Console.CombinedWriteLine("NothingButNeurons.SettingsMonitor program shutting down...");
        await SettingsMonitor.UpdateNodeStatus(Connection, "SettingsMonitor", null);
        Connection.Close();
        ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
    }
}