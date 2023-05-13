using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;
using Google.Protobuf.WellKnownTypes;

namespace NothingButNeurons.SettingsMonitor;

public class SettingsMonitor : ActorBaseWithBroadcaster
{
    private bool _processed;
    private MySqlConnection Connection;

    public SettingsMonitor(string connectionString, PID? debugServerPID = null) : base(debugServerPID)
    {
        Connection = new MySqlConnection(connectionString);
        Connection.Open();
    }

    protected override bool ReceiveMessage(IContext context)
    {
        _processed = false;

        switch (context.Message)
        {
            // Requests should be sent in the form context.RequestAsync<SettingResponseMessage>(), see Orchestrator.ServiceMonitor for example
            case SettingRequestMessage msg:
                // TODO: Change to Trace
                SendDebugMessage(DebugSeverity.Info, "Settings", $"Received SettingRequestMessage for setting {msg.Setting} in {msg.TableName} from {context.Sender}.");
                string val = GetSettingValue(msg.TableName, msg.Setting);
                if (val != null)
                {
                    // TODO: Change to Trace
                    SendDebugMessage(DebugSeverity.Info, "Settings", "Responding to SettingRequestMessage", $"Replying to request (from {context.Sender}) for setting {msg.Setting} in {msg.TableName} with value {val}.");
                    context.Respond(new SettingResponseMessage() { Value = val });
                } else
                {
                    SendDebugMessage(DebugSeverity.Warning, "Settings", "Value read for SettingRequestMessage null", $"The request for setting {msg.Setting} in {msg.TableName} returned a null value; check the table and setting (key) names.");
                }
                break;
            case UnstableHandlerException msg:
                SettingRequestMessage og = msg.FailedMessage.Unpack<SettingRequestMessage>();
                SendDebugMessage(DebugSeverity.Error, "Settings", "Exception while handling SettingRequestMessage.", $"Failed to retrieve setting {og.Setting} in {og.TableName}.");
                break;
            case NodeOnlineMessage msg:
                // TODO: Update online services database table (which should use name and PID, not port)
                // TODO: Update/create base Node actor which sends this message on online.
                AddRoutee(msg.PID);
                break;
            case NodeOfflineMessage msg:
                // TODO: Update online services database table (which should use name and PID, not port)
                // TODO: Update/create base Node actor which sends this message on shutdown. Also have Orchestrator send it when a service goes red (after updating it to watch for services coming online, either using debug message or making the node online table writing to the changelog and watching for the change setting notification)
                RemoveRoutee(msg.PID);
                break;
            default:
                break;
        }

        return _processed;
    }

    #region Lifecycle methods
    protected override void ProcessRestartingMessage(IContext context, Restarting msg)
    {
    }

    protected override void ProcessStartedMessage(IContext context, Started msg)
    {
        // Subscribe to all events in the local EventStream
        context.System.EventStream.Subscribe<Google.Protobuf.IMessage>(OnEventReceived);
    }

    protected override void ProcessStoppingMessage(IContext context, Stopping msg)
    {
        Connection.Close();
    }

    protected override void ProcessStoppedMessage(IContext context, Stopped msg)
    {
    }
    #endregion Lifecycle methods

    private void OnEventReceived(Google.Protobuf.IMessage e)
    {
        // Broadcast the Protobuf event message (setting change notification from Program.cs) to remote PIDs
        SettingChangedMessage msg = (SettingChangedMessage)e;
        SendDebugMessage(DebugSeverity.Notice, "Settings", "Settings change detected. Broadcasting to all live nodes.", $"{msg.Setting} in {msg.TableName} changed to {msg.Value}");
        Broadcast(msg);
    }

    private string? GetSettingValue(string tableName, string searchKey)
    {
        // Get the first and second column names
        var query = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND ORDINAL_POSITION IN (1, 2);";
        using var cmd = new MySqlCommand(query,Connection);
        cmd.Parameters.AddWithValue("@TableName", tableName);
        using var reader = cmd.ExecuteReader();

        var columnNames = new string[2];
        int columnIndex = 0;
        while (reader.Read())
        {
            columnNames[columnIndex++] = reader.GetString(0);
        }
        reader.Close();

        // Construct and execute the dynamic SQL query
        query = $"SELECT `{columnNames[1]}` FROM `{tableName}` WHERE `{columnNames[0]}` = @SearchKey;";
        SendDebugMessage(DebugSeverity.Info, "Settings", $"Retrieved column names; executing query: {query} with SearchKey: {searchKey}");
        cmd.CommandText = query;
        cmd.Parameters.AddWithValue("@SearchKey", searchKey);

        var result = cmd.ExecuteScalar();
        SendDebugMessage(DebugSeverity.Info, "Settings", $"Result: {result}");

        return result?.ToString();
    }
}
