global using NothingButNeurons.Shared;
global using NothingButNeurons.Shared.Messages;
global using NothingButNeurons.CCSL;
global using System.Diagnostics;
using Proto;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace NothingButNeurons.DebugLogViewer;

/// <summary>
/// MainWindow class managing the user interface and initializing the ActorSystem.
/// </summary>
public partial class MainWindow : Window
{
    ActorSystem ProtoSystem;
    PID? DebugServer;
    PID DebugUI;

    // Declare a timer for handling debug context typing
    private System.Timers.Timer DebugTypingTimer;
    private const int DebugContextTypingTimeout = 500;
    //Proto.Remote ports
    private int Port;

    /// <summary>
    /// Initializes MainWindow components, sets up DebugSeverity dropdown items, and calls InitializeActorSystem.
    /// </summary>
    public MainWindow()
    {
        // Get command-line arguments
        string[] args = Environment.GetCommandLineArgs();
        if (args.Length >= 2)
        {
            // In this app, the first argument is a dll
            args = args[1].Split(' ');
            Port = int.Parse(args[0]);
        }
        else
        {
            Port = Shared.Consts.DefaultPorts.DEBUG_LOG_VIEWER;
        }

        InitializeComponent();
        drpDebugSeverity.ItemsSource = Enum.GetValues(typeof(DebugSeverity));
        drpDebugSeverity.SelectedIndex = 2;

        InitializeActorSystem();

        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    /// <summary>
    /// Initializes the Proto.Actor system.
    /// </summary>
    private async void InitializeActorSystem()
    {
        _InitializeActorSystem();

        ProtoSystem.EventStream.Subscribe(async (SelfPortChangedMessage msg) => {
            CCSL.Console.CombinedWriteLine($"DebugLogViewer got SelfPortChangedMessage with new port: {msg.Port}. Restarting ActorSystem.");

            Port = msg.Port;
            await ProtoSystem.Remote().ShutdownAsync();
            Thread.Sleep(7000);
            _InitializeActorSystem();
        });
    }

    private async void _InitializeActorSystem()
    {
        ProtoSystem = Nodes.GetActorSystem(Port);

        DebugServer = await Nodes.GetPIDFromSettings(ProtoSystem.Root, "DebugServer");
        DebugUI = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new DebugUI(DebugServer, rtbDebug)), "DebugUI");
        Nodes.SendNodeOnline(ProtoSystem.Root, "DebugLogViewer", DebugUI);
        UpdateDebugUISubscription();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        SendDebugMessage(DebugSeverity.Trace, "Trace", "Test debug - Trace", "This is a sample debug message to test the DebgUI styling.");
    }
    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        SendDebugMessage(DebugSeverity.Debug, "Debug", "Test debug - Debug", "This is a sample debug message to test the DebgUI styling.");
    }
    private void Button_Click_2(object sender, RoutedEventArgs e)
    {
        SendDebugMessage(DebugSeverity.Info, "Info", "Test debug - Info", "This is a sample debug message to test the DebgUI styling.");
    }
    private void Button_Click_3(object sender, RoutedEventArgs e)
    {
        SendDebugMessage(DebugSeverity.Notice, "Notice", "Test debug - Notice", "This is a sample debug message to test the DebgUI styling.");
    }
    private void Button_Click_4(object sender, RoutedEventArgs e)
    {
        SendDebugMessage(DebugSeverity.Warning, "Warning", "Test debug - Warning", "This is a sample debug message to test the DebgUI styling.");
    }
    private void Button_Click_5(object sender, RoutedEventArgs e)
    {
        SendDebugMessage(DebugSeverity.Error, "Error", "Test debug - Error", "This is a sample debug message to test the DebgUI styling.");
    }
    private void Button_Click_6(object sender, RoutedEventArgs e)
    {
        SendDebugMessage(DebugSeverity.Critical, "Critical", "Test debug - Critical", "This is a sample debug message to test the DebgUI styling.");
    }
    private void Button_Click_7(object sender, RoutedEventArgs e)
    {
        SendDebugMessage(DebugSeverity.Fatal, "Fatal", "Test debug - Fatal", "This is a sample debug message to test the DebgUI styling.");
    }

    /// <summary>
    /// Sends debug message with the specified severity, context, summary, and message.
    /// </summary>
    /// <param name="severity">Severity level of the debug message.</param>
    /// <param name="context">Context of the debug message.</param>
    /// <param name="summary">Summary of the debug message.</param>
    /// <param name="message">Content of the debug message.</param>
    private void SendDebugMessage(DebugSeverity severity = DebugSeverity.Trace, string context = "", string summary = "", string message = "")
    {
        ProtoSystem.Root.Send(DebugServer, new DebugOutboundMessage
        {
            Severity = severity,
            Context = context,
            Summary = summary,
            Message = message,
            SenderClass = "",
            SenderName = "",
            SenderSystemAddr = "",
            ParentName = "",
            ParentSystemAddr = "",
            MessageSentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds()
        });
    }

    private void chkDebugEnable_Checked(object sender, RoutedEventArgs e)
    {
        UpdateDebugUISubscription();
    }

    private void drpDebugSeverity_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateDebugUISubscription();
    }

    private void txtDebugFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DebugUI is null) return;

        if (DebugTypingTimer != null)
        {
            DebugTypingTimer.Stop();
        }
        else
        {
            DebugTypingTimer = new System.Timers.Timer();
            DebugTypingTimer.Interval = DebugContextTypingTimeout;
            DebugTypingTimer.Elapsed += (sender, e) =>
            {
                DebugTypingTimer?.Stop();
                UpdateDebugUISubscription();
            };
        }
        DebugTypingTimer.Start();
    }

    /// <summary>
    /// Sends a message to update DebugUI with new settings.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void UpdateDebugUIIncludes(object sender, RoutedEventArgs e)
    {
        if (DebugUI is null) return;

        ProtoSystem.Root.Send(DebugUI, new DebugUIIncludesMessage { IncludeSenderInfo = chkShowSenderInfo.IsChecked ?? false, IncludeParentInfo = chkShowParentInfo.IsChecked ?? false, IncludeServerReceivedTime = chkShowServerTime.IsChecked ?? false });
    }

    /// <summary>
    /// Updates DebugUI subscription based on UI settings.
    /// </summary>
    private void UpdateDebugUISubscription()
    {
        if (DebugUI is null) return;

        Dispatcher.Invoke(() =>
        {
            if (!chkDebugEnable.IsChecked ?? false)
            {
                // So we stop receiving messages
                ProtoSystem.Root.Send(DebugServer, new DebugUnsubscribeMessage { Subscriber = DebugUI });
            }
            // Update display/filter according to changes
            ProtoSystem.Root.Send(DebugUI, new DebugUISubUpdateMessage
            {
                Severity = (DebugSeverity)drpDebugSeverity.SelectedItem,
                Context = txtDebugContext.Text,
                Summary = txtDebugSummary.Text,
                Message = txtDebugMessage.Text,
                SenderClass = txtDebugSenderClass.Text,
                SenderName = txtDebugSenderName.Text,
                ParentName = txtDebugParentName.Text
            });
        });
    }

    private void btnFlushDebugs_Click(object sender, RoutedEventArgs e)
    {
        ProtoSystem.Root.Send(DebugUI, new DebugFlushMessage());
    }

    private void OnProcessExit(object sender, EventArgs e)
    {
        Nodes.SendNodeOffline(ProtoSystem.Root, "DebugLogViewer");
        ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
    }
}