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
    const int QueryInterval = 10; // Seconds

    static int Port;
    static ActorSystem ProtoSystem;
    static PID Monitor;
    static MySqlConnection Connection;
    static System.Timers.Timer QueryTimer;
    static DateTime LastQueryTime;

    static void Main(string[] args)
    {
        Port = DefaultPorts.SETTINGS_MONITOR;

        CombinedWriteLine($"NothingButNeurons.SettingsMonitor program starting on port {Port}...");

        ProtoSystem = Nodes.GetActorSystem(Port);

        InitializeDbConnection();

        // Likely to be null (or inaccurately non-null) - after all, SettingsMonitor is a prereq for DebugServer in Orchestrator - but it's worth checking. And if it's non-null, we check if it is in fact online and update the status accordingly (Orchestrator will be checking/updating it, too, if it's reporting online - but only if Orchestrator is running :P)
        string query = "SELECT PID FROM NodeStatus WHERE Node = @Node;";
        using var cmd = new MySqlCommand(query, Connection);
        cmd.Parameters.AddWithValue("@Node", "DebugServer");
        string? pidString = cmd.ExecuteScalar() as string;
        PID? debugServerPID = Nodes.GetPIDFromString(pidString);
        if (debugServerPID != null)
        {
            CombinedWriteLine("DebugServer reports it is online (NodeStatus db entry exists with non-null PID), checking if it actually is...");
            ProtoSystem.Root.RequestAsync<PongMessage>(debugServerPID, new PingMessage { }, new System.Threading.CancellationToken()).WaitUpTo(TimeSpan.FromMilliseconds(850)).ContinueWith(x =>
            {
                if (x.IsFaulted || !x.Result.completed)
                {
                    CombinedWriteLine("It isn't. Setting NodeStatus offline.");
                    SettingsMonitor.UpdateNodeStatus(Connection, "DebugServer", null);
                    debugServerPID = null;
                }
                else
                {
                    CombinedWriteLine("It is.");
                }
            });

        }

        Monitor = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new SettingsMonitor(ConnectionString, debugServerPID)), "SettingsMonitor");

        CombinedWriteLine($"NothingButNeurons.SettingsMonitor program ready ({Monitor}).");

        LastQueryTime = DateTime.UtcNow.AddSeconds(-2 * QueryInterval);
        QueryTimer = new System.Timers.Timer(TimeSpan.FromSeconds(QueryInterval));
        QueryTimer.Elapsed += (s, e) => {
            GetChangesAfterAsync(LastQueryTime);
            LastQueryTime = DateTime.UtcNow;
        };
        QueryTimer.Start();

        Console.ReadLine();
        CombinedWriteLine("NothingButNeurons.SettingsMonitor program shutting down...");
        SettingsMonitor.UpdateNodeStatus(Connection, "SettingsMonitor", null).Wait();
        Connection.Close();
        ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
    }

    static void InitializeDbConnection()
    {
        Connection = new MySqlConnection(ConnectionString);
        Connection.Open();
    }

    public static async Task GetChangesAfterAsync(DateTime dateTimeUtc)
    {
        // This query uses a subquery to find the maximum changeTimestamp for each combination of tableName and setting.
        // The subquery returns only the most recent changeTimestamp for each combination.
        // The outer query then joins the original table with the subquery results to only return the rows with the most recent changeTimestamp for each combination.
        string query = "SELECT t1.tableName, t1.setting, t1.value\nFROM SettingsChangeLog t1\nINNER JOIN (\n    SELECT tableName, setting, MAX(changeTimestamp) AS maxChangeTimestamp\n    FROM SettingsChangeLog\n    WHERE changeTimestamp > @dateTimeUtc\n    GROUP BY tableName, setting\n) t2\nON t1.tableName = t2.tableName AND t1.setting = t2.setting AND t1.changeTimestamp = t2.maxChangeTimestamp";
        using MySqlCommand command = new(query, Connection);
        command.Parameters.AddWithValue("@dateTimeUtc", dateTimeUtc);

        using MySqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            string tableName = reader.GetString(0);
            string setting = reader.GetString(1);
            string value = reader.GetString(2);

            HandleSettingsEntry(tableName, setting, value);
        }
    }

    private static void HandleSettingsEntry(string tableName, string setting, string value)
    {
        CombinedWriteLine($"Setting change detected: Table: {tableName}, Setting: {setting}, Value: {value}");

        ProtoSystem.EventStream.Publish(new SettingChangedMessage() { TableName=tableName, Setting=setting, Value=value });
    }

    static void CombinedWriteLine(string line)
    {
        Debug.WriteLine(line);
        Console.WriteLine(line);
    }
}