using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;
using Google.Protobuf.WellKnownTypes;
using System.Security.Cryptography;

namespace NothingButNeurons.SettingsMonitor;

public class SettingsMonitor : ActorBaseWithBroadcaster
{
    private MySqlConnection Connection;
    private static SemaphoreSlim connectionSemaphore = new(1);

    public SettingsMonitor(string connectionString, PID? debugServerPID = null) : base(debugServerPID)
    {
        Connection = new MySqlConnection(connectionString);
        Connection.Open();
    }

    protected override bool ReceiveMessage(IContext context)
    {
        bool processed = false;

        switch (context.Message)
        {
            case SettingRequestMessage msg:
                SendDebugMessage(DebugSeverity.Trace, "Settings", $"Received SettingRequestMessage for setting {msg.Setting} in {msg.TableName} from {context.Sender}.");
                //CCSL.Console.CombinedWriteLine($"Received SettingRequestMessage for setting {msg.Setting} in {msg.TableName} from {context.Sender}.");
                string? val = GetSettingValue(msg.TableName, msg.Setting);
                if (val != null)
                {
                    SendDebugMessage(DebugSeverity.Trace, "Settings", "Responding to SettingRequestMessage", $"Replying to request (from {context.Sender}) for setting {msg.Setting} in {msg.TableName} with value {val}.");
                    //CCSL.Console.CombinedWriteLine($"Replying to request (from {context.Sender}) for setting {msg.Setting} in {msg.TableName} with value {val}.");
                    context.Respond(new SettingResponseMessage() { Value = val });
                } else
                {
                    SendDebugMessage(DebugSeverity.Warning, "Settings", "Value read for SettingRequestMessage null", $"The request for setting {msg.Setting} in {msg.TableName} returned a null value; check the table and setting (key) names.");
                    //CCSL.Console.CombinedWriteLine($"The request for setting {msg.Setting} in {msg.TableName} returned a null value; check the table and setting (key) names.");
                    context.Respond(new SettingResponseMessage());
                }
                processed = true;
                break;
            case UnstableHandlerException msg:
                SettingRequestMessage og = msg.FailedMessage.Unpack<SettingRequestMessage>();
                SendDebugMessage(DebugSeverity.Error, "Settings", "Exception while handling SettingRequestMessage.", $"Failed to retrieve setting {og.Setting} in {og.TableName}.");
                processed = true;
                break;
            case NodeOnlineMessage msg:
                {
                    UpdateNodeStatus(Connection, msg.Name, msg.PID);

                    AddRoutee(msg.PID);
                    processed = true;
                    break;
                }
            case NodeOfflineMessage msg:
                {
                    UpdateNodeStatus(Connection, msg.Name, null); 

                    RemoveRoutee(msg.PID);
                    processed = true;
                    break;
                }
            default:
                break;
        }

        return processed;
    }

    #region Lifecycle methods
    protected override void ProcessRestartingMessage(IContext context, Restarting msg)
    {
    }

    protected override void ProcessStartedMessage(IContext context, Started msg)
    {
        // Subscribe to all events in the local EventStream
        context.System.EventStream.Subscribe<Google.Protobuf.IMessage>(OnEventReceived);

        UpdateNodeStatus(Connection, "SettingsMonitor", SelfPID);
    }

    protected override void ProcessStoppingMessage(IContext context, Stopping msg)
    {
        UpdateNodeStatus(Connection, "SettingsMonitor", null).Wait();

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
        connectionSemaphore.Wait();
        try
        {

            // Get the first and second column names
            var query = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND ORDINAL_POSITION IN (1, 2);";
            using var cmd = new MySqlCommand(query, Connection);
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
            if (result is DBNull)
                result = null;
            SendDebugMessage(DebugSeverity.Info, "Settings", $"Result: {result}");

            return result?.ToString();
        }
        catch (Exception ex)
        {
            CCSL.Console.CombinedWriteLine($"GetSettingValue got exception: {ex.ToString()}");
            return null;
        }
        finally
        {
            connectionSemaphore.Release();
        }
    }

    internal static async Task UpdateNodeStatus(MySqlConnection connection, string name, PID? pid)
    {
        await connectionSemaphore.WaitAsync();
        try
        {
            string query = @"INSERT INTO NodeStatus (Node, PID) VALUES (@Node, @PID) 
                ON DUPLICATE KEY UPDATE PID = @PID;";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Node", name);
            cmd.Parameters.AddWithValue("@PID", pid == null ? DBNull.Value : string.IsNullOrEmpty(pid.ToString()) ? DBNull.Value : pid.ToString());
            cmd.Prepare();
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            CCSL.Console.CombinedWriteLine($"UpdateNodeStatus got exception: {ex.ToString()}");
        }
        finally
        {
            connectionSemaphore.Release();
        }
    }
}
