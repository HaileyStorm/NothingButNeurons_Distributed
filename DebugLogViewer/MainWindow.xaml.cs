global using NothingButNeurons.Shared;
global using NothingButNeurons.Shared.Messages;
global using NothingButNeurons.CCSL;
global using System.Diagnostics;
using NothingButNeurons.Brain.Regions.Neurons;
using Proto;
using Proto.Timers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using Google.Protobuf;
using Grpc.Net.Client;
using System.IO.Compression;
using Grpc.Net.Compression;

namespace NothingButNeurons.DebugLogViewer;

/// <summary>
/// MainWindow class managing the user interface and initializing the ActorSystem.
/// </summary>
public partial class MainWindow : Window
{
    ActorSystem ProtoSystem;
    PID DebugServer;
    PID DebugUI;
    PID HiveMind;

    // Declare a timer for handling debug context typing
    private System.Timers.Timer DebugTypingTimer;
    private const int DebugContextTypingTimeout = 500;
    //Proto.Remote ports
    private int Port;
    private int DebugServerPort;
    //TODO: Temporary, does not belong here.
    private int HiveMindPort;

    /// <summary>
    /// Initializes MainWindow components, sets up DebugSeverity dropdown items, and calls InitializeActorSystem.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        drpDebugSeverity.ItemsSource = Enum.GetValues(typeof(DebugSeverity));
        drpDebugSeverity.SelectedIndex = 2;

        InitializeActorSystem();
    }

    /// <summary>
    /// Initializes the Proto.Actor system.
    /// </summary>
    private void InitializeActorSystem()
    {
        // Get command-line arguments
        string[] args = Environment.GetCommandLineArgs();
        if (args.Length >= 3) // will be 2
        {
            Port = int.Parse(args[0]);
            DebugServerPort = int.Parse(args[1]);
            //TODO: Temporary, does not belong here.
            HiveMindPort = int.Parse(args[2]);
        }
        else
        {
            Port = 8003;
            DebugServerPort = 8001;
            HiveMindPort = 8000;
        }

        var remoteConfig = GrpcNetRemoteConfig
            .BindToLocalhost(Port)
            .WithProtoMessages(DebuggerReflection.Descriptor, NeuronsReflection.Descriptor, IOReflection.Descriptor)
            /*.WithChannelOptions(new GrpcChannelOptions
            {
                CompressionProviders = new[]
                        {
                            new GzipCompressionProvider(CompressionLevel.Fastest)
                        }
            })*/
            .WithRemoteDiagnostics(true);
        ProtoSystem = new ActorSystem().WithRemote(remoteConfig);
        ProtoSystem.Remote().StartAsync();
        while (!ProtoSystem.Remote().Started)
        {
            Thread.Sleep(100);
        }

        DebugServer = PID.FromAddress($"127.0.0.1:{DebugServerPort}", "DebugServer");
        DebugUI = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new DebugUI(DebugServer, rtbDebug)), "DebugUI");
        UpdateDebugUISubscription();


        // *****************
        // TODO: Move to Designer. All the way til inputNeuronTimer.Start()
        // *****************
        var random = new System.Random();

        List<NeuronData> neurons = new()
        {
            new(new NeuronAddress(1, 789), AccumulationFunction.Sum, 0d, ActivationFunction.TanH, -0.09408328947811073d, 0d, 0.3102968627042596d, ResetFunction.Hold),
            new(new NeuronAddress(2, 600), AccumulationFunction.Product, 0.21092151536762183d, ActivationFunction.TanH, 0.5810408471041231d, 0d, 0.3756515834738581d, ResetFunction.Zero),
            new(new NeuronAddress(3, 100), AccumulationFunction.Sum, 0d, ActivationFunction.TanH, 0d, 0d, 0.23604621666228753d, ResetFunction.Clamp1),
            new(new NeuronAddress(3, 444), AccumulationFunction.Sum, -0.06435270007257976d, ActivationFunction.Identity, 2.2217183878210376d, 0d, 0d, ResetFunction.Zero),
            new(new NeuronAddress(4, 800), AccumulationFunction.Sum, -0.7751348713404214d, ActivationFunction.TanH, 0.22254413860750688d, 0d, 0.4308581751064845d, ResetFunction.Zero),
            new(new NeuronAddress(5, 150), AccumulationFunction.Sum, 0.06435270007257965d, ActivationFunction.TanH, 0.8339034132770404d, 0d, 0.15656961151675708d, ResetFunction.Inverse),
            new(new NeuronAddress(7, 250), AccumulationFunction.Product, -0.15816826832005448d, ActivationFunction.Gauss, -0.5036150896731022, 0d, 0.5691418248935155d, ResetFunction.Zero),
            new(new NeuronAddress(7, 512), AccumulationFunction.Sum, -0.6972573007778182d, ActivationFunction.TanH, -0.6618229161312104d, 0d, 0.5220185133018942d, ResetFunction.Clamp1),
            new(new NeuronAddress(7, 555), AccumulationFunction.Sum, 0.2683961526980623d, ActivationFunction.Clamp, 2.785568175044677d, 0d, 0.07647637600944429d, ResetFunction.Zero),
            new(new NeuronAddress(9, 200), AccumulationFunction.Sum, -0.2683961526980625d, ActivationFunction.SoftP, -1.7631583456920181d, 0d, 0.47798148669810575d, ResetFunction.Zero),
            new(new NeuronAddress(10, 20), AccumulationFunction.Sum, -0.6195143149204596d, ActivationFunction.TanH, 1.539525432336215d, 0d, 0.7639537833377125d, ResetFunction.Half),
            new(new NeuronAddress(11, 123), AccumulationFunction.Product, -0.4687066603212533d, ActivationFunction.ReLu, 2.6754208091457112d, 0d, 0.6897031372957403d, ResetFunction.Hold),
            new(new NeuronAddress(12, 333), AccumulationFunction.Sum, -0.3307299830628878d, ActivationFunction.TanH, -0.15767329484039294d, 0d, 0d, ResetFunction.Zero),
            new(new NeuronAddress(13, 356), AccumulationFunction.Sum, -0.6972573007778182d, ActivationFunction.TanH, 1.0200662315024553d, 0d, 0.5220185133018942d, ResetFunction.Zero),
            new(new NeuronAddress(13, 980), AccumulationFunction.Sum, -0.8521343495571289d, ActivationFunction.SiLu, 1.763158345692018d, 0d, 0.47798148669810575d, ResetFunction.Zero),
            new(new NeuronAddress(15, 929), AccumulationFunction.Sum, -0.21092151536762183d, ActivationFunction.TanH, -1.7631583456920181d, 0d, 0.3756515834738581d, ResetFunction.Zero),
        };
        byte[] neuronData = neurons.ToByteArray();
        /* Debug.WriteLine("Created neuronData: ");
         foreach (byte b in neuronData)
         {
             string binary = Convert.ToString(b, 2).PadLeft(8, '0');
             Debug.WriteLine(binary);
         }*/

        //Debug.WriteLine($"\nActivation thresholds: {NeuronBase.BitsToDouble(4, 15, 0d, 1d)},{NeuronBase.BitsToDouble(5, 15, 0d, 1d)},{NeuronBase.BitsToDouble(3, 15, 0d, 1d)},{NeuronBase.BitsToDouble(0, 15, 0d, 1d)},{NeuronBase.BitsToDouble(6, 15, 0d, 1d)},{NeuronBase.BitsToDouble(2, 15, 0d, 1d)},{NeuronBase.BitsToDouble(9, 15, 0d, 1d)},{NeuronBase.BitsToDouble(8, 15, 0d, 1d)},{NeuronBase.BitsToDouble(1, 15, 0d, 1d)},{NeuronBase.BitsToDouble(7, 15, 0d, 1d)},{NeuronBase.BitsToDouble(12, 15, 0d, 1d)},{NeuronBase.BitsToDouble(11, 15, 0d, 1d)},{NeuronBase.BitsToDouble(0, 15, 0d, 1d)},{NeuronBase.BitsToDouble(8, 15, 0d, 1d)},{NeuronBase.BitsToDouble(7, 15, 0d, 1d)},{NeuronBase.BitsToDouble(5, 15, 0d, 1d)},\n");
        byte[] synapseData = new List<int>
        {
            new SynapseBitField(1, 789, 9, 200, 13).Data.Data.ReverseBytes(),
            new SynapseBitField(1, 789, 15, 929, 0).Data.Data.ReverseBytes(),

            new SynapseBitField(2, 600, 7, 250, 14).Data.Data.ReverseBytes(),
            new SynapseBitField(2, 600, 13, 980, 2).Data.Data.ReverseBytes(),

            new SynapseBitField(3, 100, 9, 200, 4).Data.Data.ReverseBytes(),
            new SynapseBitField(3, 100, 10, 20, 12).Data.Data.ReverseBytes(),
            new SynapseBitField(3, 100, 15, 929, 15).Data.Data.ReverseBytes(),

            new SynapseBitField(3, 444, 12, 333, 2).Data.Data.ReverseBytes(),
            new SynapseBitField(3, 444, 13, 356, 11).Data.Data.ReverseBytes(),
            new SynapseBitField(3, 444, 15, 929, 5).Data.Data.ReverseBytes(),

            new SynapseBitField(4, 800, 7, 512, 5).Data.Data.ReverseBytes(),
            new SynapseBitField(4, 800, 13, 980, 13).Data.Data.ReverseBytes(),

            new SynapseBitField(5, 150, 7, 555, 1).Data.Data.ReverseBytes(),
            new SynapseBitField(5, 150, 12, 333, 10).Data.Data.ReverseBytes(),

            new SynapseBitField(7, 250, 11, 123, 1).Data.Data.ReverseBytes(),

            new SynapseBitField(7, 512, 11, 123, 3).Data.Data.ReverseBytes(),
            new SynapseBitField(7, 512, 13, 356, 12).Data.Data.ReverseBytes(),

            new SynapseBitField(7, 555, 11, 123, 15).Data.Data.ReverseBytes(),

            new SynapseBitField(9, 200, 7, 555, 0).Data.Data.ReverseBytes(),
            new SynapseBitField(9, 200, 10, 20, 6).Data.Data.ReverseBytes(),
            new SynapseBitField(9, 200, 13, 980, 4).Data.Data.ReverseBytes(),

            new SynapseBitField(10, 20, 10, 20, 3).Data.Data.ReverseBytes(),
            new SynapseBitField(10, 20, 15, 929, 1).Data.Data.ReverseBytes(),

            new SynapseBitField(11, 123, 7, 512, 4).Data.Data.ReverseBytes(),
            new SynapseBitField(11, 123, 10, 20, 10).Data.Data.ReverseBytes(),

            new SynapseBitField(12, 333, 7, 250, 10).Data.Data.ReverseBytes(),
            new SynapseBitField(12, 333, 7, 555, 5).Data.Data.ReverseBytes(),
        }.AsEnumerable().SelectMany(BitConverter.GetBytes).ToArray();
        /*Debug.WriteLine("Created synapseData: ");
        foreach (byte b in synapseData)
        {
            string binary = Convert.ToString(b, 2).PadLeft(8, '0');
            Debug.WriteLine(binary);
        }*/

        HiveMind = PID.FromAddress($"127.0.0.1:{HiveMindPort}", "HiveMind");
        ProtoSystem.Root.Send(HiveMind, new SpawnBrainMessage { NeuronData = ByteString.CopyFrom(neuronData), SynapseData = ByteString.CopyFrom(synapseData) });
        // TODO: Instead of sleeping, use SpawnBrainAck (will have to send SpawnBrainMessage from an actor, as it sends the act to the sender of the spawn)
        Thread.Sleep(1200);
        ProtoSystem.Root.Send(HiveMind, new ActivateHiveMindMessage());

        int tickTime = 300;
        Scheduler scheduler = new(ProtoSystem.Root);
        scheduler.SendRepeatedly(TimeSpan.FromMilliseconds(tickTime), HiveMind, new TickMessage());

        System.Timers.Timer inputNeuronTimer = new(70);
        inputNeuronTimer.Elapsed += (s, e) =>
        {
            if (random.Next(0, 15) == 0)
                ProtoSystem.Root.Send(new PID("127.0.0.1:8000", "HiveMind/Brain$1/1/789"), new SignalMessage { Val = random.NextDouble() * 2d - 1d });
            if (random.Next(0, 15) == 1)
                ProtoSystem.Root.Send(new PID("127.0.0.1:8000", "HiveMind/Brain$1/2/600"), new SignalMessage { Val = random.NextDouble() * 2d - 1d });
            if (random.Next(0, 15) == 2)
                ProtoSystem.Root.Send(new PID("127.0.0.1:8000", "HiveMind/Brain$1/3/100"), new SignalMessage { Val = random.NextDouble() * 2d - 1d });
            if (random.Next(0, 15) == 3)
                ProtoSystem.Root.Send(new PID("127.0.0.1:8000", "HiveMind/Brain$1/3/444"), new SignalMessage { Val = random.NextDouble() * 2d - 1d });
            if (random.Next(0, 15) == 4)
                ProtoSystem.Root.Send(new PID("127.0.0.1:8000", "HiveMind/Brain$1/4/800"), new SignalMessage { Val = random.NextDouble() * 2d - 1d });
            if (random.Next(0, 15) == 5)
                ProtoSystem.Root.Send(new PID("127.0.0.1:8000", "HiveMind/Brain$1/5/150"), new SignalMessage { Val = random.NextDouble() * 2d - 1d });
            
        };
        inputNeuronTimer.Start();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        SendDebugMessage(DebugSeverity.Trace, "Trace", "Test debug - Trace", "This is a sample debug message to test the DebgUI styling.");
    }
    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        SendDebugMessage(DebugSeverity.Info, "Info", "Test debug - Info", "This is a sample debug message to test the DebgUI styling.");
    }
    private void Button_Click_2(object sender, RoutedEventArgs e)
    {
        SendDebugMessage(DebugSeverity.Debug, "Debug", "Test debug - Debug", "This is a sample debug message to test the DebgUI styling.");
    }
    private void Button_Click_3(object sender, RoutedEventArgs e)
    {
        SendDebugMessage(DebugSeverity.Warning, "Warning", "Test debug - Warning", "This is a sample debug message to test the DebgUI styling.");
    }
    private void Button_Click_4(object sender, RoutedEventArgs e)
    {
        SendDebugMessage(DebugSeverity.Alert, "Alert", "Test debug - Alert", "This is a sample debug message to test the DebgUI styling.");
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
        SendDebugMessage(DebugSeverity.Test, "Test", "Test debug - Test", "This is a sample debug message to test the DebgUI styling.");
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
}