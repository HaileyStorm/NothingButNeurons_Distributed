global using NothingButNeurons.Shared;
global using NothingButNeurons.Shared.Messages;
global using NothingButNeurons.CCSL;
global using System.Diagnostics;
using NothingButNeurons.IO;
using NothingButNeurons.Brain.Regions.Neurons;
using Proto;
using Proto.Timers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace NothingButNeurons.Debugger;

/// <summary>
/// MainWindow class managing the user interface and initializing the ActorSystem.
/// </summary>
public partial class MainWindow : Window
{
    // Declare fields for ActorSystem and actor PIDs
    ActorSystem ProtoSystem;
    PID TestActor;
    PID DebugServer;
    PID DebugFileWriter;
    PID DebugUI;
    PID NetworkVisualizationUpdator;
    PID HiveMind;

    // Declare a timer for handling debug context typing
    private System.Timers.Timer DebugTypingTimer;
    private const int DebugContextTypingTimeout = 500;

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
    /// Initializes the Proto.Actor system, creates actors, sets up neurons/synapses, sends activation messages, and schedules periodic tick and signal messages.
    /// </summary>
    private void InitializeActorSystem()
    {
        int tickTime = 300;

        var random = new System.Random();

        ProtoSystem = new ActorSystem();

        DebugServer = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new DebugServer()), "DebugServer");
        DebugUI = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new DebugUI(rtbDebug)), "DebugUI");
        UpdateDebugUISubscription();
        DebugFileWriter = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new DebugFileWriter()), "DebugFileWriter");
        SendDebugMessage(DebugSeverity.Trace, "Startup", "ActorSystem, DebugServer, DebugFileWriter and DebugUI created.");

        NetworkVisualizationUpdator = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new Debugger.NetworkVisualization.Updater(networkVisualizationCanvas, tickTime)), "NetworkVisualizationUpdator");

        /*var watch = new Stopwatch();
        var rnd = new Random();
        foreach (ResetFunction func in ((ResetFunction[])Enum.GetValues(typeof(ResetFunction))).OrderBy(x => rnd.Next()))
        {
            watch.Restart();
            for (int i = 0; i < 2500; i++)
            {
                func.Reset(0.2d, 0.4d, 0.6d, 20);
            }
            watch.Stop();
            SendDebugMessage(DebugSeverity.Test, "ResetFunction Test", $"{func}\t{watch.ElapsedMilliseconds}");
        }*/

        HiveMind = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new HiveMind()), "HiveMind");

        List<int> neurons = new()
        {
            new NeuronPart1BitField(1, 789, AccumulationFunction.Sum, 16, ActivationFunction.TanH, 30).Data.Data,
            new NeuronPart2BitField(32, 4, ResetFunction.Hold).Data.Data,

            new NeuronPart1BitField(2, 600, AccumulationFunction.Product, 20, ActivationFunction.TanH, 40).Data.Data,
            new NeuronPart2BitField(32, 5, ResetFunction.Zero).Data.Data,



            /*new NeuronPart1BitField(3, 0, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 1, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 2, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 3, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 4, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 5, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 6, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 7, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 8, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 9, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,

            new NeuronPart1BitField(3, 10, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 11, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 12, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 13, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 14, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 15, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 16, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 17, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 18, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 19, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,

            new NeuronPart1BitField(3, 20, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 21, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 22, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 23, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 24, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 25, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 26, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 27, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 28, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 29, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,

            new NeuronPart1BitField(3, 30, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 31, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 32, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 33, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 34, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 35, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 36, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 37, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 38, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 39, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,

            new NeuronPart1BitField(3, 40, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 41, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 42, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 43, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 44, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 45, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 46, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 47, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 48, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 49, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,

            new NeuronPart1BitField(3, 50, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 51, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 52, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 53, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 54, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 55, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 56, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 57, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 58, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,
            new NeuronPart1BitField(3, 59, AccumulationFunction.Sum, 14, ActivationFunction.TanH, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,*/



            new NeuronPart1BitField(3, 100, AccumulationFunction.Sum, 15, ActivationFunction.TanH, 32).Data.Data,
            new NeuronPart2BitField(32, 3, ResetFunction.Clamp1).Data.Data,

            new NeuronPart1BitField(3, 444, AccumulationFunction.Sum, 14, ActivationFunction.Identity, 56).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,

            new NeuronPart1BitField(4, 800, AccumulationFunction.Sum, 3, ActivationFunction.TanH, 35).Data.Data,
            new NeuronPart2BitField(32, 6, ResetFunction.Zero).Data.Data,

            new NeuronPart1BitField(5, 150, AccumulationFunction.Sum, 17, ActivationFunction.TanH, 43).Data.Data,
            new NeuronPart2BitField(32, 2, ResetFunction.Inverse).Data.Data,

            new NeuronPart1BitField(7, 250, AccumulationFunction.Product, 12, ActivationFunction.Gauss, 24).Data.Data,
            new NeuronPart2BitField(32, 9, ResetFunction.Zero).Data.Data,

            new NeuronPart1BitField(7, 512, AccumulationFunction.Sum, 4, ActivationFunction.TanH, 22).Data.Data,
            new NeuronPart2BitField(32, 8, ResetFunction.Clamp1).Data.Data,

            new NeuronPart1BitField(7, 555, AccumulationFunction.Sum, 21, ActivationFunction.Clamp, 61).Data.Data,
            new NeuronPart2BitField(32, 1, ResetFunction.Zero).Data.Data,

            new NeuronPart1BitField(9, 200, AccumulationFunction.Sum, 10, ActivationFunction.SoftP, 11).Data.Data,
            new NeuronPart2BitField(32, 7, ResetFunction.Zero).Data.Data,

            new NeuronPart1BitField(10, 20, AccumulationFunction.Sum, 5, ActivationFunction.TanH, 50).Data.Data,
            new NeuronPart2BitField(32, 12, ResetFunction.Half).Data.Data,

            new NeuronPart1BitField(11, 123, AccumulationFunction.Product, 7, ActivationFunction.ReLU, 60).Data.Data,
            new NeuronPart2BitField(32, 11, ResetFunction.Hold).Data.Data,

            new NeuronPart1BitField(12, 333, AccumulationFunction.Sum, 9, ActivationFunction.TanH, 29).Data.Data,
            new NeuronPart2BitField(32, 0, ResetFunction.Zero).Data.Data,

            new NeuronPart1BitField(13, 356, AccumulationFunction.Sum, 4, ActivationFunction.TanH, 45).Data.Data,
            new NeuronPart2BitField(32, 8, ResetFunction.Zero).Data.Data,

            new NeuronPart1BitField(13, 980, AccumulationFunction.Sum, 2, ActivationFunction.SiLU, 52).Data.Data,
            new NeuronPart2BitField(32, 7, ResetFunction.Zero).Data.Data,

            new NeuronPart1BitField(15, 929, AccumulationFunction.Sum, 11, ActivationFunction.TanH, 11).Data.Data,
            new NeuronPart2BitField(32, 5, ResetFunction.Zero).Data.Data,
        };
        byte[] neuronData = new byte[neurons.Count * 3];
        int resultIndex = 0;
        byte[] currentIntBytes;
        for (int i = 0; i < neurons.Count; i++)
        {
            int currentInt = neurons[i];
            int bytesToTake = i % 2 == 0 ? 4 : 2;
            if (bytesToTake == 4)
            {
                currentIntBytes = BitConverter.GetBytes(currentInt).Reverse().ToArray();
            }
            else
            {
                currentIntBytes = BitConverter.GetBytes((short)currentInt).Reverse().ToArray();
            }
            Array.Copy(currentIntBytes, 0, neuronData, resultIndex, bytesToTake);
            resultIndex += bytesToTake;
        }
        /* Debug.WriteLine("Created neuronData: ");
         foreach (byte b in neuronData)
         {
             string binary = Convert.ToString(b, 2).PadLeft(8, '0');
             Debug.WriteLine(binary);
         }*/
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
        ProtoSystem.Root.Send(HiveMind, new SpawnBrainMessage(neuronData, synapseData));
        ProtoSystem.Root.Send(HiveMind, new ActivateHiveMindMessage());

        Scheduler scheduler = new(ProtoSystem.Root);
        scheduler.SendRepeatedly(TimeSpan.FromMilliseconds(tickTime), HiveMind, new TickMessage());

        Thread.Sleep(2000);
        // TODO: This is a random number ONCE, not every message, can't use scheduler for that
        /*scheduler.SendRepeatedly(TimeSpan.FromMilliseconds(random.Next(100, 2000)), new PID(ProtoSystem.Address, "HiveMind/Brain$1/3/100"), new SignalMessage(random.NextDouble()));
        scheduler.SendRepeatedly(TimeSpan.FromMilliseconds(random.Next(100, 2000)), new PID(ProtoSystem.Address, "HiveMind/Brain$1/1/789"), new SignalMessage(random.NextDouble()));
        scheduler.SendRepeatedly(TimeSpan.FromMilliseconds(random.Next(100, 2000)), new PID(ProtoSystem.Address, "HiveMind/Brain$1/2/600"), new SignalMessage(random.NextDouble()));
        scheduler.SendRepeatedly(TimeSpan.FromMilliseconds(random.Next(100, 2000)), new PID(ProtoSystem.Address, "HiveMind/Brain$1/4/800"), new SignalMessage(random.NextDouble()));
        scheduler.SendRepeatedly(TimeSpan.FromMilliseconds(random.Next(100, 2000)), new PID(ProtoSystem.Address, "HiveMind/Brain$1/5/150"), new SignalMessage(random.NextDouble()));
        scheduler.SendRepeatedly(TimeSpan.FromMilliseconds(random.Next(100, 2000)), new PID(ProtoSystem.Address, "HiveMind/Brain$1/3/444"), new SignalMessage(random.NextDouble()));*/
        System.Timers.Timer inputNeuronTimer = new(100);
        inputNeuronTimer.Elapsed += (s, e) =>
        {
            if (random.Next(0, 15) == 0)
                ProtoSystem.Root.Send(new PID(ProtoSystem.Address, "HiveMind/Brain$1/3/100"), new SignalMessage(random.NextDouble() * 2d - 1d));
            if (random.Next(0, 15) == 0)
                ProtoSystem.Root.Send(new PID(ProtoSystem.Address, "HiveMind/Brain$1/1/789"), new SignalMessage(random.NextDouble() * 2d - 1d));
            if (random.Next(0, 15) == 0)
                ProtoSystem.Root.Send(new PID(ProtoSystem.Address, "HiveMind/Brain$1/2/600"), new SignalMessage(random.NextDouble() * 2d - 1d));
            if (random.Next(0, 15) == 0)
                ProtoSystem.Root.Send(new PID(ProtoSystem.Address, "HiveMind/Brain$1/4/800"), new SignalMessage(random.NextDouble() * 2d - 1d));
            if (random.Next(0, 15) == 0)
                ProtoSystem.Root.Send(new PID(ProtoSystem.Address, "HiveMind/Brain$1/5/150"), new SignalMessage(random.NextDouble() * 2d - 1d));
            if (random.Next(0, 15) == 0)
                ProtoSystem.Root.Send(new PID(ProtoSystem.Address, "HiveMind/Brain$1/3/444"), new SignalMessage(random.NextDouble() * 2d - 1d));
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
        ProtoSystem.Root.Send(DebugServer, new DebugOutboundMessage(severity, context, summary, message, "", "", "", "", "", DateTimeOffset.Now.ToUnixTimeMilliseconds()));
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

        ProtoSystem.Root.Send(DebugUI, new DebugUIIncludesMessage(chkShowSenderInfo.IsChecked ?? false, chkShowParentInfo.IsChecked ?? false, chkShowServerTime.IsChecked ?? false));
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
                ProtoSystem.Root.Send(DebugServer, new DebugUnsubscribeMessage(DebugUI));
            }
            // Update display/filter according to changes
            ProtoSystem.Root.Send(DebugUI, new DebugUISubUpdateMessage((DebugSeverity)drpDebugSeverity.SelectedItem, txtDebugContext.Text, txtDebugSummary.Text, txtDebugMessage.Text, txtDebugSenderClass.Text, txtDebugSenderName.Text, txtDebugParentName.Text));
        });
    }

    private void btnFlushDebugs_Click(object sender, RoutedEventArgs e)
    {
        ProtoSystem.Root.Send(DebugUI, new DebugFlushMessage());
    }

    private void btnFlushLogFile_Click(object sender, RoutedEventArgs e)
    {
        if (DebugFileWriter is null) return;
        ProtoSystem.Root.Send(DebugFileWriter, new DebugFlushMessage());
    }
}

/// <summary>
/// Decider class with a static method to determine how to handle actor failure.
/// </summary>
internal class Decider
{
    /// <summary>
    /// Returns SupervisorDirective.Restart, determining how to handle actor failure.
    /// </summary>
    /// <param name="pid">The PID of the failed actor.</param>
    /// <param name="reason">The exception that caused the actor failure.</param>
    /// <returns>SupervisorDirective.Restart</returns>
    public static SupervisorDirective Decide(PID pid, Exception reason)
    {
        return SupervisorDirective.Restart;
    }
}
