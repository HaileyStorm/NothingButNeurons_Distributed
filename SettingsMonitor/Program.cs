global using NothingButNeurons.Shared;
global using NothingButNeurons.Shared.Messages;
global using NothingButNeurons.CCSL;
global using System.Diagnostics;
using Proto;
using Proto.Remote.GrpcNet;
using Proto.Remote;
using MySqlConnector;
using System.Data;

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
        Port = Shared.Consts.DefaultPorts.SETTINGS_MONITOR;

        CombinedWriteLine($"NothingButNeurons.SettingsMonitor program starting on port {Port}...");

        ProtoSystem = Nodes.GetActorSystem(Port);

        // TODO: Read DebugServer running state / pid from db (pass null if not running)
        int debugServerPort = Shared.Consts.DefaultPorts.DEBUG_SERVER;
        PID? debugServerPID = null;
        if (debugServerPort != null)
            debugServerPID = PID.FromAddress($"127.0.0.1:{debugServerPort}", "DebugServer");
        Monitor = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new SettingsMonitor(ConnectionString, debugServerPID)), "SettingsMonitor");

        CombinedWriteLine($"NothingButNeurons.SettingsMonitor program ready ({Monitor}).");

        InitializeDbConnection();
        LastQueryTime = DateTime.UtcNow.AddSeconds(-2 * QueryInterval);
        QueryTimer = new System.Timers.Timer(TimeSpan.FromSeconds(QueryInterval));
        QueryTimer.Elapsed += (s, e) => {
            GetChangesAfterAsync(LastQueryTime);
            LastQueryTime = DateTime.UtcNow;
        };
        QueryTimer.Start();


        Console.ReadLine();
        CombinedWriteLine("NothingButNeurons.SettingsMonitor program shutting down...");
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