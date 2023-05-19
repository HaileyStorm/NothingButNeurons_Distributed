global using NothingButNeurons.Shared;
global using NothingButNeurons.Shared.Messages;
global using NothingButNeurons.CCSL;
global using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System;
using Google.Protobuf;
using Proto;
using System.Threading;
using Proto.Remote.GrpcNet;
using Proto.Remote;
using Microsoft.Win32;
using System.IO;
using Proto.Timers;
using System.Collections.Generic;
using NothingButNeurons.Shared.DataClasses.Neurons;
using System.Linq;

namespace NothingButNeurons.Designer;

public partial class MainWindow : Window
{
    private int TickTime = 300;
    private CancellationTokenSource TickCanceller;

    ActorSystem ProtoSystem;
    PID? HiveMind;

    private bool _startIsActive = false;
    private byte[] _neuronData;
    private byte[] _synapseData;
    private Random _random;

    private int Port;

    private PID? Brain = null;

    private System.Timers.Timer InputNeuronTimer;

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
            Port = Shared.Consts.DefaultPorts.DESIGNER;
        }

        InitializeComponent();
        DataContext = new MainWindowViewModel();
        TickTime = int.Parse(txtTickTime.Text);

        _random = new System.Random();

        InitializeActorSystem();

        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private async void InitializeActorSystem()
    {
        _InitializeActorSystem();

        ProtoSystem.EventStream.Subscribe(async (SelfPortChangedMessage msg) => {
            CCSL.Console.CombinedWriteLine($"Designer got SelfPortChangedMessage with new port: {msg.Port}. Restarting ActorSystem.");

            Port = msg.Port;
            await ProtoSystem.Remote().ShutdownAsync();
            Thread.Sleep(5000);
            _InitializeActorSystem();
        });
    }

    private async void _InitializeActorSystem()
    {
        ProtoSystem = Nodes.GetActorSystem(Port);

        HiveMind = await Nodes.GetPIDFromSettings(ProtoSystem.Root, "IO");
        if (HiveMind == null)
        {
            throw new Exception("IO node must be online to run Designer.");
        }

        PID pid = ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new DesignerHelper()), "DesignerHelper");
        Nodes.SendNodeOnline(ProtoSystem.Root, "Designer", pid);
    }

    private void LoadBrainFromFile_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "Nothings files (*.nbn)|*.nbn",
            InitialDirectory = Directory.GetCurrentDirectory() // Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (openFileDialog.ShowDialog() == true)
        {
            string fileName = openFileDialog.FileName;

            (_neuronData, _synapseData) = Shared.Serialization.Brain.ReadDataFromFile(fileName);

            SetFileLight(true);
            SetGenLight(false);
        }
    }

    private void GenerateSave_Click(object sender, RoutedEventArgs e)
    {
        // Get the region/neuron parameters from GUI and ensure they're in range
        int numInputRegions = Math.Clamp(int.Parse(NumInputRegions.Text), 1, 5);
        int inputRegionsNumNeuronsMin = Math.Clamp(int.Parse(InputRegionsNumNeuronsMin.Text), 1, Math.Min(1024, int.Parse(InputRegionsNumNeuronsMax.Text)));
        int inputRegionsNumNeuronsMax = Math.Clamp(int.Parse(InputRegionsNumNeuronsMax.Text), Math.Max(1, int.Parse(InputRegionsNumNeuronsMin.Text)), 1024);
        int numInteriorRegions = Math.Clamp(int.Parse(NumInteriorRegions.Text), 1, 7);
        int interiorRegionsNumNeuronsMin = Math.Clamp(int.Parse(InteriorRegionsNumNeuronsMin.Text), 1, Math.Min(1024, int.Parse(InteriorRegionsNumNeuronsMax.Text)));
        int interiorRegionsNumNeuronsMax = Math.Clamp(int.Parse(InteriorRegionsNumNeuronsMax.Text), Math.Max(1, int.Parse(InteriorRegionsNumNeuronsMin.Text)), 1024);
        int numOutputRegions = Math.Clamp(int.Parse(NumOutputRegions.Text), 1, 3);
        int outputRegionsNumNeuronsMin = Math.Clamp(int.Parse(OutputRegionsNumNeuronsMin.Text), 1, Math.Min(1024, int.Parse(OutputRegionsNumNeuronsMax.Text)));
        int outputRegionsNumNeuronsMax = Math.Clamp(int.Parse(OutputRegionsNumNeuronsMax.Text), Math.Max(1, int.Parse(OutputRegionsNumNeuronsMin.Text)), 1024);
        string accumulationFunction = AccumulationFunctionDropdown.Text;
        double preActivationThresholdMin = Math.Clamp(double.Parse(PreActivationThresholdMin.Text), -1, Math.Min(1, double.Parse(PreActivationThresholdMax.Text)));
        double preActivationThresholdMax = Math.Clamp(double.Parse(PreActivationThresholdMax.Text), Math.Max(-1, double.Parse(PreActivationThresholdMin.Text)), 1);
        string activationFunction = ActivationFunctionDropdown.Text;
        double activationParameterAMin = Math.Clamp(double.Parse(ActivationParameterAMin.Text), -1, Math.Min(1, double.Parse(ActivationParameterAMax.Text)));
        double activationParameterAMax = Math.Clamp(double.Parse(ActivationParameterAMax.Text), Math.Max(-1, double.Parse(ActivationParameterAMin.Text)), 1);
        double activationParameterBMin = Math.Clamp(double.Parse(ActivationParameterBMin.Text), -1, Math.Min(1, double.Parse(ActivationParameterBMax.Text)));
        double activationParameterBMax = Math.Clamp(double.Parse(ActivationParameterBMax.Text), Math.Max(-1, double.Parse(ActivationParameterBMin.Text)), 1);
        double activationThresholdMin = Math.Clamp(double.Parse(ActivationThresholdMin.Text), -1, Math.Min(1, double.Parse(ActivationThresholdMax.Text)));
        double activationThresholdMax = Math.Clamp(double.Parse(ActivationThresholdMax.Text), Math.Max(-1, double.Parse(ActivationThresholdMin.Text)), 1);
        string resetFunction = ResetFunctionDropdown.Text;

        var neurons = RandomBrain.GenerateRandomNeurons(numInputRegions, inputRegionsNumNeuronsMin, inputRegionsNumNeuronsMax, numInteriorRegions, interiorRegionsNumNeuronsMin, interiorRegionsNumNeuronsMax, numOutputRegions, outputRegionsNumNeuronsMin, outputRegionsNumNeuronsMax, accumulationFunction, preActivationThresholdMin, preActivationThresholdMax, activationFunction, activationParameterAMin, activationParameterAMax, activationParameterBMin, activationParameterBMax, activationThresholdMin, activationThresholdMax, resetFunction);
        //ValidateToNeuronDataListFunction(neurons);
        _neuronData = neurons.ToByteArray();

        // Get the synapse parameters from GUI and ensure they're in range
        int inputRegionsNumSynapsesPerNeuronMin = Math.Clamp(int.Parse(InputRegionsNumSynapsesPerNeuronMin.Text), 1, int.Parse(InputRegionsNumSynapsesPerNeuronMax.Text));
        int inputRegionsNumSynapsesPerNeuronMax = Math.Max(int.Parse(InputRegionsNumSynapsesPerNeuronMax.Text), int.Parse(InputRegionsNumSynapsesPerNeuronMin.Text));
        double inputRegionsSynapseStrengthMin = Math.Clamp(double.Parse(InputRegionsSynapseStrengthMin.Text), -1, Math.Min(1, double.Parse(InputRegionsSynapseStrengthMax.Text)));
        double inputRegionsSynapseStrengthMax = Math.Clamp(double.Parse(InputRegionsSynapseStrengthMax.Text), Math.Max(-1, double.Parse(InputRegionsSynapseStrengthMin.Text)), 1);
        int interiorRegionsNumSynapsesPerNeuronMin = Math.Clamp(int.Parse(InteriorRegionsNumSynapsesPerNeuronMin.Text), 1, int.Parse(InteriorRegionsNumSynapsesPerNeuronMax.Text));
        int interiorRegionsNumSynapsesPerNeuronMax = Math.Max(int.Parse(InteriorRegionsNumSynapsesPerNeuronMax.Text), int.Parse(InteriorRegionsNumSynapsesPerNeuronMin.Text));
        double interiorRegionsSynapseStrengthMin = Math.Clamp(double.Parse(InteriorRegionsSynapseStrengthMin.Text), -1, Math.Min(1, double.Parse(InteriorRegionsSynapseStrengthMax.Text)));
        double interiorRegionsSynapseStrengthMax = Math.Clamp(double.Parse(InteriorRegionsSynapseStrengthMax.Text), Math.Max(-1, double.Parse(InteriorRegionsSynapseStrengthMin.Text)), 1);

        var synapses = RandomBrain.GenerateRandomSynapses(neurons, inputRegionsNumSynapsesPerNeuronMin, inputRegionsNumSynapsesPerNeuronMax, inputRegionsSynapseStrengthMin, inputRegionsSynapseStrengthMax, interiorRegionsNumSynapsesPerNeuronMin, interiorRegionsNumSynapsesPerNeuronMax, interiorRegionsSynapseStrengthMin, interiorRegionsSynapseStrengthMax);
        //Debug.WriteLine($"\nSynapses validated? {ValidateToSynapseDataListFunction(synapses)}");
        _synapseData = synapses.ToByteArray();

        SetFileLight(false);
        SetGenLight(true);

        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            Filter = "Nothings files (*.nbn)|*.nbn",
            InitialDirectory = Directory.GetCurrentDirectory() // Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (saveFileDialog.ShowDialog() == true)
        {
            string fileName = saveFileDialog.FileName;
            Shared.Serialization.Brain.WriteDataToFile(fileName, _neuronData, _synapseData);
        }
    }

    private void SetFileLight(bool isActive)
    {
        if (isActive)
        {
            SpawnButton.IsEnabled = true;
            StartStopButton.IsEnabled = true;
        }
        FileLoadedLight.Fill = isActive ? Brushes.Green : Brushes.Gray;
    }

    private void SetGenLight(bool isActive)
    {
        if (isActive)
        {
            SpawnButton.IsEnabled = true;
            StartStopButton.IsEnabled = true;
        }
        GenLoadedLight.Fill = isActive ? Brushes.Green : Brushes.Gray;
    }

    private async void Spawn_Click(object sender, RoutedEventArgs e)
    {
        if (_neuronData == null || _neuronData.Length == 0)
            return;

        Brain = (await ProtoSystem.Root.RequestAsync<SpawnBrainAckMessage>(HiveMind!, new SpawnBrainMessage { NeuronData = ByteString.CopyFrom(_neuronData), SynapseData = ByteString.CopyFrom(_synapseData) }, TimeSpan.FromMilliseconds(2500))).BrainPID;
        CCSL.Console.CombinedWriteLine($"Spawned brain: {Brain}");
        ProtoSystem.Root.Send(HiveMind!, new ActivateHiveMindMessage());

        // TODO: Does this belong in IO project or ... ? Or perhaps ActivateHiveMind should take a tick time and HiveMind should use that to start sending Ticks to itself?
        Scheduler scheduler = new(ProtoSystem.Root);
        TickCanceller = scheduler.SendRepeatedly(TimeSpan.FromMilliseconds(TickTime), HiveMind!, new TickMessage());

        SetSpawnLight(true);
    }

    private void SetSpawnLight(bool isActive)
    {
        SpawnLight.Fill = isActive ? Brushes.Green : Brushes.Gray;
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        // Get list of brains from HiveMind, etc.
    }

    private void StartStop_Click(object sender, RoutedEventArgs e)
    {
        if (_neuronData == null || _neuronData.Length == 0)
            return;

        var lastSignalTime = new Dictionary<string, DateTime>(); // Store the last signal time for each neuron
        var nextSignalDuration = new Dictionary<string, TimeSpan>(); // Store the next random signal duration for each neuron

        _startIsActive = !_startIsActive;

        // Start
        if (_startIsActive)
        {
            var neuronPIDs = NeuronDataExtensions.ByteArrayToNeuronDataList(_neuronData).Where(n => n.Address.RegionPart < 6).ToList().ConvertAll(n => $"{Brain!.Id}/{n.Address.RegionPart}/{n.Address.NeuronPart}");

            // Initialize lastSignalTime and nextSignalDuration dictionaries
            foreach (var pid in neuronPIDs)
            {
                lastSignalTime[pid] = DateTime.Now;
                nextSignalDuration[pid] = TimeSpan.FromMilliseconds(_random.Next(int.Parse(MinSignalPeriod.Text), int.Parse(MaxSignalPeriod.Text) + 1));
            }

            InputNeuronTimer = new(10);
            InputNeuronTimer.Elapsed += (s, elapsedEventArgs) =>
            {
                MinSignalPeriod.Dispatcher.Invoke(() =>
                {
                    foreach (var pid in neuronPIDs)
                    {
                        if (DateTime.Now - lastSignalTime[pid] >= nextSignalDuration[pid])
                        {
                            ProtoSystem.Root.Send(new PID(HiveMind!.Address, pid), new SignalMessage { Val = _random.NextDouble() * (double.Parse(MaxSignalValue.Text) - double.Parse(MinSignalValue.Text)) + double.Parse(MinSignalValue.Text) });
                            lastSignalTime[pid] = DateTime.Now;
                            nextSignalDuration[pid] = TimeSpan.FromMilliseconds(_random.Next(int.Parse(MinSignalPeriod.Text), int.Parse(MaxSignalPeriod.Text) + 1));
                        }
                    }
                });
            };
            InputNeuronTimer.Start();

            StartStopButton.Content = "Stop";
            SetStartStopLight(true);
        }
        // Stop
        else
        {
            InputNeuronTimer.Stop();

            StartStopButton.Content = "Start";
            SetStartStopLight(false);
        }
    }

    private void SetStartStopLight(bool isActive)
    {
        StartStopLight.Fill = isActive ? Brushes.Green : Brushes.Gray;
    }

    public static void ValidateToNeuronDataListFunction(List<NeuronData> originalNeuronDataList)
    {
        originalNeuronDataList.Sort(new NeuronDataExtensions.NeuronDataComparer());
        // Convert the original List<NeuronData> to byte array
        byte[] neuronBytes = originalNeuronDataList.ToByteArray();

        // Convert the byte array back to List<NeuronData>
        List<NeuronData> reconstructedNeuronDataList = NeuronDataExtensions.ByteArrayToNeuronDataList(neuronBytes);

        // Compare the original and reconstructed List<NeuronData>
        bool areEqual = originalNeuronDataList.Count == reconstructedNeuronDataList.Count;
        int i;
        for (i = 0; i < originalNeuronDataList.Count && areEqual; i++)
        {
            NeuronData originalNeuron = originalNeuronDataList[i];
            NeuronData reconstructedNeuron = reconstructedNeuronDataList[i];

            /*Debug.WriteLine($"{originalNeuron.Address.RegionPart} vs {reconstructedNeuron.Address.RegionPart}");
            Debug.WriteLine($"{originalNeuron.Address.NeuronPart} vs {reconstructedNeuron.Address.NeuronPart}");
            Debug.WriteLine($"{originalNeuron.AccumulationFunction} vs {reconstructedNeuron.AccumulationFunction}");
            Debug.WriteLine($"{originalNeuron.PreActivationThreshold} vs {reconstructedNeuron.PreActivationThreshold}");
            Debug.WriteLine($"{originalNeuron.ActivationFunction} vs {reconstructedNeuron.ActivationFunction}");
            Debug.WriteLine($"{originalNeuron.ActivationParameterA} vs {reconstructedNeuron.ActivationParameterA}");
            Debug.WriteLine($"{originalNeuron.ActivationParameterB} vs {reconstructedNeuron.ActivationParameterB}");
            Debug.WriteLine($"{originalNeuron.ActivationThreshold} vs {reconstructedNeuron.ActivationThreshold}");
            Debug.WriteLine($"{originalNeuron.ResetFunction} vs {reconstructedNeuron.ResetFunction}");*/
            areEqual &= originalNeuron.Address.NeuronPart == reconstructedNeuron.Address.NeuronPart;
            areEqual &= originalNeuron.Address.RegionPart == reconstructedNeuron.Address.RegionPart;
            areEqual &= originalNeuron.AccumulationFunction == reconstructedNeuron.AccumulationFunction;
            areEqual &= Math.Abs(originalNeuron.PreActivationThreshold - reconstructedNeuron.PreActivationThreshold) < 0.1;
            areEqual &= originalNeuron.ActivationFunction == reconstructedNeuron.ActivationFunction;
            areEqual &= Math.Abs(originalNeuron.ActivationParameterA - reconstructedNeuron.ActivationParameterA) < 0.1;
            areEqual &= Math.Abs(originalNeuron.ActivationParameterB - reconstructedNeuron.ActivationParameterB) < 0.1;
            areEqual &= Math.Abs(originalNeuron.ActivationThreshold - reconstructedNeuron.ActivationThreshold) < 0.1;
            areEqual &= originalNeuron.ResetFunction == reconstructedNeuron.ResetFunction;
        }

        Debug.WriteLine($"\nValidation result: {areEqual}, got to {i} of {originalNeuronDataList.Count} \n");
    }

    public static bool ValidateToSynapseDataListFunction(List<SynapseData> originalSynapseDataList)
    {
        originalSynapseDataList.Sort(new SynapseDataExtensions.SynapseDataComparer());
        byte[] synapseBytes = originalSynapseDataList.ToByteArray();
        List<SynapseData> convertedSynapseDataList = SynapseDataExtensions.ByteArrayToSynapseDataList(synapseBytes);

        if (originalSynapseDataList.Count != convertedSynapseDataList.Count)
        {
            Debug.WriteLine("Count mismatch");
            return false;
        }

        for (int i = 0; i < originalSynapseDataList.Count; i++)
        {
            SynapseData originalSynapseData = originalSynapseDataList[i];
            SynapseData convertedSynapseData = convertedSynapseDataList[i];

            /*Debug.WriteLine($"{originalSynapseData.FromAddress.RegionPart} vs {convertedSynapseData.FromAddress.RegionPart}");
            Debug.WriteLine($"{originalSynapseData.FromAddress.NeuronPart} vs {convertedSynapseData.FromAddress.NeuronPart}");
            Debug.WriteLine($"{originalSynapseData.ToAddress.RegionPart} vs {convertedSynapseData.ToAddress.RegionPart}");
            Debug.WriteLine($"{originalSynapseData.ToAddress.NeuronPart} vs {convertedSynapseData.ToAddress.NeuronPart}");
            Debug.WriteLine($"{originalSynapseData.Strength} vs {convertedSynapseData.Strength}");*/
            if (originalSynapseData.FromAddress.NeuronPart != convertedSynapseData.FromAddress.NeuronPart ||
                originalSynapseData.FromAddress.RegionPart != convertedSynapseData.FromAddress.RegionPart ||
                originalSynapseData.ToAddress.NeuronPart != convertedSynapseData.ToAddress.NeuronPart ||
                originalSynapseData.ToAddress.RegionPart != convertedSynapseData.ToAddress.RegionPart ||
                Math.Abs(originalSynapseData.Strength - convertedSynapseData.Strength) > 0.1)
            {
                Debug.WriteLine($"Failed at {i}");
                return false;
            }
        }

        return true;
    }

    private void txtTickTime_TextChanged(object sender, TextChangedEventArgs e)
    {
        int newTime = int.Parse(txtTickTime.Text);
        if (SpawnLight.Fill == Brushes.Green && TickCanceller != null && !TickCanceller.IsCancellationRequested && newTime != TickTime)
        {
            TickCanceller.Cancel();
            Scheduler scheduler = new(ProtoSystem.Root);
            TickCanceller = scheduler.SendRepeatedly(TimeSpan.FromMilliseconds(newTime), HiveMind!, new TickMessage());
        }
        TickTime = newTime;
    }

    private void OnProcessExit(object sender, EventArgs e)
    {
        Nodes.SendNodeOffline(ProtoSystem.Root, "Designer");
        ProtoSystem.Remote().ShutdownAsync().GetAwaiter().GetResult();
    }
}
