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

namespace NothingButNeurons.Designer
{
    public partial class MainWindow : Window
    {
        private const int TickTime = 300;

        ActorSystem ProtoSystem;
        PID HiveMind;

        private bool _startIsActive = false;
        private byte[] _neuronData;
        private byte[] _synapseData;
        private Random _random;

        private int Port;
        private int HiveMindPort;

        private System.Timers.Timer InputNeuronTimer;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();

            _random = new System.Random();

            InitializeActorSystem();
        }

        private void InitializeActorSystem()
        {
            // Get command-line arguments
            string[] args = Environment.GetCommandLineArgs();
            Console.WriteLine($"\nCommand line args: {string.Join(',', args)}");
            if (args.Length >= 2)
            {
                // In this app, the first argument is a dll
                args = args[1].Split(' ');
                Port = int.Parse(args[0]);
                HiveMindPort = int.Parse(args[1]);
                Console.WriteLine($"Parsed ports: {Port}, {HiveMindPort}");
            }
            else
            {
                Port = Shared.Consts.DefaultPorts.DESIGNER;
                HiveMindPort = Shared.Consts.DefaultPorts.IO;
                Console.WriteLine($"Default ports: {Port}, {HiveMindPort}");
            }

            ProtoSystem = Nodes.GetActorSystem(Port);

            ProtoSystem.Root.SpawnNamed(Props.FromProducer(() => new DesignerHelper()), "DesignerHelper");
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
            /*List<NeuronData> neurons = new()
        {
            // Intentionally out of order to test the sorting in ToByteArray()
            new(new NeuronAddress(15, 929), AccumulationFunction.Sum, -0.21092151536762183d, ActivationFunction.TanH, -1.7631583456920181d, 0d, 0.3756515834738581d, ResetFunction.Zero),
            new(new NeuronAddress(1, 789), AccumulationFunction.Sum, 0d, ActivationFunction.TanH, -0.09408328947811073d, 0d, 0.3102968627042596d, ResetFunction.Hold),
            new(new NeuronAddress(7, 512), AccumulationFunction.Sum, -0.6972573007778182d, ActivationFunction.TanH, -0.6618229161312104d, 0d, 0.5220185133018942d, ResetFunction.Clamp1),
            new(new NeuronAddress(2, 600), AccumulationFunction.Product, -0.15816826832005448d, ActivationFunction.TanH, 0.5810408471041231d, 0d, 0.3756515834738581d, ResetFunction.Zero),
            new(new NeuronAddress(3, 100), AccumulationFunction.Sum, 0d, ActivationFunction.TanH, 0d, 0d, 0.23604621666228753d, ResetFunction.Clamp1),
            new(new NeuronAddress(10, 20), AccumulationFunction.Sum, -0.6195143149204596d, ActivationFunction.TanH, 1.539525432336215d, 0d, 0.7639537833377125d, ResetFunction.Half),
            new(new NeuronAddress(3, 444), AccumulationFunction.Sum, -0.06435270007257976d, ActivationFunction.Identity, 2.2217183878210376d, 0d, 0d, ResetFunction.Zero),
            new(new NeuronAddress(4, 800), AccumulationFunction.Sum, -0.7751348713404214d, ActivationFunction.TanH, 0.22254413860750688d, 0d, 0.4308581751064845d, ResetFunction.Zero),
            new(new NeuronAddress(13, 980), AccumulationFunction.Sum, -0.8521343495571289d, ActivationFunction.SiLu, 1.763158345692018d, 0d, 0.47798148669810575d, ResetFunction.Zero),
            new(new NeuronAddress(5, 150), AccumulationFunction.Sum, 0.06435270007257965d, ActivationFunction.TanH, 0.8339034132770404d, 0d, 0.15656961151675708d, ResetFunction.Inverse),
            new(new NeuronAddress(7, 250), AccumulationFunction.Product, -0.15816826832005448d, ActivationFunction.Gauss, -0.5036150896731022, 0d, 0.5691418248935155d, ResetFunction.Zero),
            new(new NeuronAddress(7, 555), AccumulationFunction.Sum, 0.2683961526980623d, ActivationFunction.Clamp, 2.785568175044677d, 0d, 0.07647637600944429d, ResetFunction.Zero),
            new(new NeuronAddress(9, 200), AccumulationFunction.Sum, -0.2683961526980625d, ActivationFunction.SoftP, -1.7631583456920181d, 0d, 0.47798148669810575d, ResetFunction.Zero),
            new(new NeuronAddress(11, 123), AccumulationFunction.Product, -0.4687066603212533d, ActivationFunction.ReLu, 2.6754208091457112d, 0d, 0.6897031372957403d, ResetFunction.Hold),
            new(new NeuronAddress(12, 333), AccumulationFunction.Sum, -0.3307299830628878d, ActivationFunction.TanH, -0.15767329484039294d, 0d, 0d, ResetFunction.Zero),
            new(new NeuronAddress(13, 356), AccumulationFunction.Sum, -0.6972573007778182d, ActivationFunction.TanH, 1.0200662315024553d, 0d, 0.5220185133018942d, ResetFunction.Zero),
        };
        byte[] neuronData = neurons.ToByteArray();*/
            /* Debug.WriteLine("Created neuronData: ");
             foreach (byte b in neuronData)
             {
                 string binary = Convert.ToString(b, 2).PadLeft(8, '0');
                 Debug.WriteLine(binary);
             }*/

            /*byte[] synapseData = new List<SynapseData>()
            {
                // Intentionally out of order to test the sorting in ToByteArray()
                new SynapseData(new NeuronAddress(11, 123), new NeuronAddress(7, 512), 0.24869683305228385),
                new SynapseData(new NeuronAddress(1, 789), new NeuronAddress(9, 200), 0.6868607769664858d),
                new SynapseData(new NeuronAddress(2, 600), new NeuronAddress(7, 250), 0.8470472479811115d),
                new SynapseData(new NeuronAddress(2, 600), new NeuronAddress(13, 980), -0.6868607769664858d),
                new SynapseData(new NeuronAddress(3, 100), new NeuronAddress(15, 929), 1d),
                new SynapseData(new NeuronAddress(3, 444), new NeuronAddress(12, 333), -0.6868607769664858d),
                new SynapseData(new NeuronAddress(3, 444), new NeuronAddress(13, 356), 0.3794062745914806d),
                new SynapseData(new NeuronAddress(1, 789), new NeuronAddress(15, 929), -1d),
                new SynapseData(new NeuronAddress(3, 444), new NeuronAddress(15, 929), -0.24869683305228385d),
                new SynapseData(new NeuronAddress(4, 800), new NeuronAddress(7, 512), 0.6868607769664858d),
                new SynapseData(new NeuronAddress(4, 800), new NeuronAddress(13, 980), -0.8470472479811114d),
                new SynapseData(new NeuronAddress(5, 150), new NeuronAddress(7, 555), 0.24869683305228385d),
                new SynapseData(new NeuronAddress(5, 150), new NeuronAddress(12, 333), -0.8470472479811114d),
                new SynapseData(new NeuronAddress(3, 100), new NeuronAddress(9, 200), -0.3794062745914808d),
                new SynapseData(new NeuronAddress(3, 100), new NeuronAddress(10, 20), 0.5279075666754249d),
                new SynapseData(new NeuronAddress(7, 250), new NeuronAddress(11, 123), -0.5279075666754249d),
                new SynapseData(new NeuronAddress(9, 200), new NeuronAddress(10, 20), -0.3794062745914808d),
                new SynapseData(new NeuronAddress(7, 512), new NeuronAddress(11, 123), 0.5279075666754249d),
                new SynapseData(new NeuronAddress(9, 200), new NeuronAddress(7, 555), -0.138283649787031d),
                new SynapseData(new NeuronAddress(7, 512), new NeuronAddress(13, 356), 1d),
                new SynapseData(new NeuronAddress(7, 555), new NeuronAddress(11, 123), -1d),
                new SynapseData(new NeuronAddress(9, 200), new NeuronAddress(13, 980), -0.5279075666754249d),
                new SynapseData(new NeuronAddress(10, 20), new NeuronAddress(10, 20), -0.8470472479811114),
                new SynapseData(new NeuronAddress(12, 333), new NeuronAddress(7, 555), 0.6868607769664858d),
                new SynapseData(new NeuronAddress(10, 20), new NeuronAddress(15, 929), -0.3794062745914808),
                new SynapseData(new NeuronAddress(11, 123), new NeuronAddress(10, 20), 0.24869683305228385),
                new SynapseData(new NeuronAddress(12, 333), new NeuronAddress(7, 250), -0.24869683305228385),
            }.ToByteArray();*/
            /*Debug.WriteLine("Created synapseData: ");
            foreach (byte b in synapseData)
            {
                string binary = Convert.ToString(b, 2).PadLeft(8, '0');
                Debug.WriteLine(binary);
            }*/

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Nothings files (*.nbn)|*.nbn",
                InitialDirectory = Directory.GetCurrentDirectory() // Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string fileName = saveFileDialog.FileName;
                Shared.Serialization.Brain.WriteDataToFile(fileName, _neuronData, _synapseData);

                SetFileLight(false);
                SetGenLight(true);
            }
        }

        private void SetFileLight(bool isActive)
        {
            FileLoadedLight.Fill = isActive ? Brushes.Green : Brushes.Gray;
        }

        private void SetGenLight(bool isActive)
        {
            GenLoadedLight.Fill = isActive ? Brushes.Green : Brushes.Gray;
        }

        private void Spawn_Click(object sender, RoutedEventArgs e)
        {
            if (_neuronData == null || _neuronData.Length == 0)
                return;

            HiveMind = PID.FromAddress($"127.0.0.1:{HiveMindPort}", "HiveMind");
            ProtoSystem.Root.Send(HiveMind, new SpawnBrainMessage { NeuronData = ByteString.CopyFrom(_neuronData), SynapseData = ByteString.CopyFrom(_synapseData) });
            // TODO: Instead of sleeping, use SpawnBrainAck (will have to send SpawnBrainMessage from an actor, as it sends the act to the sender of the spawn)
            Thread.Sleep(1200);
            ProtoSystem.Root.Send(HiveMind, new ActivateHiveMindMessage());

            // TODO: Does this belong in IO project or ... ? Or perhaps ActivateHiveMind should take a tick time and HiveMind should use that to start sending Ticks to itself?
            Scheduler scheduler = new(ProtoSystem.Root);
            scheduler.SendRepeatedly(TimeSpan.FromMilliseconds(TickTime), HiveMind, new TickMessage());

            SetSpawnLight(true);
        }

        private void SetSpawnLight(bool isActive)
        {
            SpawnLight.Fill = isActive ? Brushes.Green : Brushes.Gray;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            // Get list of brains from HiveMind
        }

        private void StartStop_Click(object sender, RoutedEventArgs e)
        {
            _startIsActive = !_startIsActive;

            // Start
            if (_startIsActive)
            {
                InputNeuronTimer = new(70);
                InputNeuronTimer.Elapsed += (s, e) =>
                {
                    if (_random.Next(0, 15) == 0)
                        ProtoSystem.Root.Send(new PID($"127.0.0.1:{HiveMindPort}", "HiveMind/Brain$1/1/789"), new SignalMessage { Val = _random.NextDouble() * 2d - 1d });
                    if (_random.Next(0, 15) == 1)
                        ProtoSystem.Root.Send(new PID($"127.0.0.1:{HiveMindPort}", "HiveMind/Brain$1/2/600"), new SignalMessage { Val = _random.NextDouble() * 2d - 1d });
                    if (_random.Next(0, 15) == 2)
                        ProtoSystem.Root.Send(new PID($"127.0.0.1:{HiveMindPort}", "HiveMind/Brain$1/3/100"), new SignalMessage { Val = _random.NextDouble() * 2d - 1d });
                    if (_random.Next(0, 15) == 3)
                        ProtoSystem.Root.Send(new PID($"127.0.0.1:{HiveMindPort}", "HiveMind/Brain$1/3/444"), new SignalMessage { Val = _random.NextDouble() * 2d - 1d });
                    if (_random.Next(0, 15) == 4)
                        ProtoSystem.Root.Send(new PID($"127.0.0.1:{HiveMindPort}", "HiveMind/Brain$1/4/800"), new SignalMessage { Val = _random.NextDouble() * 2d - 1d });
                    if (_random.Next(0, 15) == 5)
                        ProtoSystem.Root.Send(new PID($"127.0.0.1:{HiveMindPort}", "HiveMind/Brain$1/5/150"), new SignalMessage { Val = _random.NextDouble() * 2d - 1d });
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
    }
}
