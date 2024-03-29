﻿using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Collections.Concurrent;
using System.Windows.Threading;
using System.Threading;
using System.Diagnostics;

namespace NothingButNeurons.Visualizer.NetworkVisualization;

internal partial class Updater : NodeBase
{
    private Canvas _networkVisualizationCanvas;

    private Dictionary<string, RegionInfo> _regions = new Dictionary<string, RegionInfo>();
    private Dictionary<string, NeuronInfo> _neurons = new Dictionary<string, NeuronInfo>();
    private Dictionary<string, Ellipse> _neuronEllipses = new Dictionary<string, Ellipse>();
    private Dictionary<string, ConnectionInfo> _connections = new Dictionary<string, ConnectionInfo>();
    private Dictionary<(byte, ushort), List<ConnectionInfo>> _pendingConnections = new Dictionary<(byte, ushort), List<ConnectionInfo>>();
    private Dictionary<string, Path> _connectionPaths = new Dictionary<string, Path>();
    private Random _rng;
    private int connectionTimeout;
    private bool hasInteriorRegions = false;
    private ConcurrentQueue<DebugInboundMessage> _messageQueue = new ConcurrentQueue<DebugInboundMessage>();
    private DispatcherTimer _messageProcessingTimer;
    private SemaphoreSlim _messageProcessingSemaphore = new SemaphoreSlim(1, 1);
    private Dictionary<string, double> _neuronUpdates = new Dictionary<string, double>();
    private Dictionary<string, double> _connectionUpdates = new Dictionary<string, double>();
    private readonly object connectionLock = new object();

    public Updater(PID? debugServerPID, Canvas networkVisualizationCanvas, int tickInterval) : base(debugServerPID, "Visualizer")
    {
        _networkVisualizationCanvas = networkVisualizationCanvas;
        _rng = new Random();
        connectionTimeout = (int)Math.Round(tickInterval * 0.95d);

        _messageProcessingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(connectionTimeout / 4)
        };
        _messageProcessingTimer.Tick += ProcessMessageQueueAsync;
        _messageProcessingTimer.Start();
    }

    protected override bool ReceiveMessage(IContext context)
    {
        bool processed = true;
        if (context.Message is not DebugInboundMessage msg || context.Sender == SelfPID || msg.SenderName == "NetworkVisualizationUpdater")
            return false;

        _messageQueue.Enqueue((DebugInboundMessage)context.Message);
        processed = true;
        return processed;
    }

    private async void ProcessMessageQueueAsync(object sender, EventArgs e)
    {
        _messageProcessingTimer.Stop();

        await _messageProcessingSemaphore.WaitAsync();

        try
        {
            while (_messageQueue.TryDequeue(out DebugInboundMessage msg))
            {
                ProcessMessage(msg);
            }

            _networkVisualizationCanvas.Dispatcher.Invoke(() =>
            {
                //CCSL.Console.CombinedWriteLine("Calling ApplyNeuronUpdates and ApplyConnectionUpdates");
                ApplyNeuronUpdates();
                ApplyConnectionUpdates();

                _neuronUpdates.Clear();
                _connectionUpdates.Clear();
            });
        }
        finally
        {
            _messageProcessingSemaphore.Release();
        }

        _messageProcessingTimer.Start();
    }

    private void ProcessMessage(DebugInboundMessage msg)
    {
        if (msg.Context == "Spawn")
        {
            string[] summaryParts = msg.Summary.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (msg.Summary.Contains("spawned input region") || msg.Summary.Contains("spawned interior region") || msg.Summary.Contains("spawned output region"))
            {
                string regionPid = summaryParts[5];
                regionPid = regionPid.TrimEnd('.');

                RegionType regionType = summaryParts[3] switch
                {
                    "input" => RegionType.Input,
                    "interior" => RegionType.Interior,
                    "output" => RegionType.Output,
                    _ => throw new ArgumentOutOfRangeException()
                };
                if (regionType == RegionType.Interior)
                    hasInteriorRegions = true;

                string[] messageParts = msg.Message.Split(' ');

                byte regionAddress = byte.Parse(messageParts[4]);
                int neuronCount = int.Parse(messageParts[7]);

                if (!_regions.ContainsKey(regionPid))
                {
                    RegionInfo region = new RegionInfo(regionPid, regionType, regionAddress, neuronCount);
                    _regions[regionPid] = region;
                }
                else
                {
                    _regions[regionPid].Type = regionType;
                    _regions[regionPid].Address = regionAddress;
                    _regions[regionPid].NeuronCount = neuronCount;
                }
            }
            else if (msg.Summary.Contains("spawned neuron"))
            {
                string regionPid = summaryParts[1];
                string neuronPid = summaryParts[4];
                neuronPid = neuronPid.TrimEnd('.');

                string[] messageParts = msg.Message.Split(' ');
                ushort neuronAddress = ushort.Parse(messageParts[4]);
                int synapseCount = int.Parse(messageParts[7]);

                NeuronInfo neuron = new NeuronInfo(neuronPid, neuronAddress, new Point(0, 0));
                _neurons[neuronPid] = neuron;
                // We received a spawn neuron debug before its spawn region debug
                if (!_regions.ContainsKey(regionPid))
                {
                    // Placeholder, will be replaced when the spawn region debug received
                    _regions[regionPid] = new RegionInfo(regionPid, RegionType.Input, 0, 1);
                }
                _regions[regionPid].AddNeuron(neuron);

                // Process pending connections for this neuron
                (byte, ushort) key = (_regions[regionPid].Address, neuron.Address);
                if (_pendingConnections.ContainsKey(key))
                {
                    foreach (ConnectionInfo pendingConnection in _pendingConnections[key])
                    {
                        NeuronInfo sourceNeuron = _neurons.Values.FirstOrDefault(n => n.Id == pendingConnection.SourceNeuronPid);

                        if (sourceNeuron != null)
                        {
                            pendingConnection.SetSourceNeuron(sourceNeuron);
                            pendingConnection.SetTargetNeuron(neuron); // Update target neuron in the pending connection
                            _connections[pendingConnection.Id] = pendingConnection;
                            sourceNeuron.AddConnection(pendingConnection);
                        }
                    }

                    // Remove the processed pending connections
                    _pendingConnections.Remove(key);
                }
            }
            else if (msg.Summary.Contains("spawned a synapse/axon"))
            {
                string sourceNeuronPid = summaryParts[1];

                string[] messageParts = msg.Message.Split(new char[] { ' ', '/' });
                byte targetRegionAddress = byte.Parse(messageParts[6]);
                ushort targetNeuronAddress = ushort.Parse(messageParts[7]);
                double connectionStrength = double.Parse(messageParts[10]);

                NeuronInfo targetNeuron = _regions.Values
                    .Where(r => r.Address == targetRegionAddress)
                    .SelectMany(r => r.Neurons)
                    .FirstOrDefault(n => n.Address == targetNeuronAddress);

                ConnectionInfo connection = new ConnectionInfo(sourceNeuronPid, targetNeuron, connectionStrength, connectionTimeout, ResetPathColor);

                // If the target neuron is found, set it in the connection.
                if (targetNeuron != null)
                {
                    connection.SetTargetNeuron(targetNeuron);
                }

                NeuronInfo sourceNeuron = null; // For compiler, which doesn't recognize the else below as assuring this value is assigned otherwise.
                if (_neurons.ContainsKey(sourceNeuronPid))
                {
                    sourceNeuron = _neurons[sourceNeuronPid];
                    connection.SetSourceNeuron(sourceNeuron);
                }

                if (!_neurons.ContainsKey(sourceNeuronPid) || targetNeuron == null)
                {
                    // Store the connection info in the pending connections list
                    (byte, ushort) key = (targetRegionAddress, targetNeuronAddress);
                    if (!_pendingConnections.ContainsKey(key))
                    {
                        _pendingConnections[key] = new List<ConnectionInfo>();
                    }
                    _pendingConnections[key].Add(connection);
                }
                else
                {
                    // Store the connection
                    _connections[connection.Id] = connection;
                    sourceNeuron!.AddConnection(connection);
                }
            }
            // Brain active message.
            else if (msg.Summary.Contains("active"))
            {
                // Draw the initial network visualization
                Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    DrawRegionsAndNeurons();
                });
            }
        }
        else if (msg.Context == "Signal" && msg.Message.StartsWith("Signal is from "))
        {
            //CCSL.Console.CombinedWriteLine("Synapse.");
            string toPid = msg.Summary.Split(' ')[5];
            string fromPid = msg.Message.Split(' ')[3];
            double signalStrength = double.Parse(msg.Message.Split(' ')[14]);

            //CCSL.Console.CombinedWriteLine($"\tCalling UpdateConnection with: {fromPid}-{toPid}, {signalStrength}");
            UpdateConnection($"{fromPid}-{toPid}", signalStrength);
        }
        else if (msg.Context == "Signal" && msg.Message.StartsWith("New SignalBuffer"))
        {
            //CCSL.Console.CombinedWriteLine("Neuron Signal.");
            string neuronPid = msg.Summary.Split(" ")[1];
            double signalBuffer = double.Parse(msg.Message.Split(' ')[2]);

            //CCSL.Console.CombinedWriteLine($"\tCalling UpdateNeuron with: {neuronPid}, {signalBuffer}");
            UpdateNeuron(neuronPid, signalBuffer);
        }
        else if (msg.Context == "Tick" && msg.Summary.EndsWith("Activated & Reset"))
        {
            //CCSL.Console.CombinedWriteLine("Neuron Reset.");
            string neuronPid = msg.Summary.Split(" ")[1];
            double signalBuffer = double.Parse(msg.Message.Split(' ')[7].TrimEnd('.'));

            //CCSL.Console.CombinedWriteLine($"\tCalling UpdateNeuron with: {neuronPid}, {signalBuffer}");
            UpdateNeuron(neuronPid, signalBuffer);
        }
    }

    protected override void ProcessRestartingMessage(IContext context, Restarting msg)
    {
    }

    protected override void ProcessStartedMessage(IContext context, Started msg)
    {
        // TODO: Specific (filter by severity, brain, etc.).
        SubscribeToDebugs();
    }

    protected override void ProcessStoppingMessage(IContext context, Stopping msg)
    {
    }

    protected override void ProcessStoppedMessage(IContext context, Stopped msg)
    {
    }
}