using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Timers;
using System.Windows;

namespace NothingButNeurons.Visualizer.NetworkVisualization;

public enum RegionType
{
    Input,
    Interior,
    Output
}

public class RegionInfo
{
    public string Id { get; set; }
    public byte Address { get; set; }
    public RegionType Type { get; set; }
    public int NeuronCount { get; set; }
    public List<NeuronInfo> Neurons { get; set; }

    public RegionInfo(string id, RegionType type, byte address, int neuronCount)
    {
        Id = id;
        Type = type;
        Address = address;
        NeuronCount = neuronCount;
        Neurons = new List<NeuronInfo>();
    }

    public void AddNeuron(NeuronInfo neuron)
    {
        Neurons.Add(neuron);
    }
}

public class NeuronInfo
{
    public string Id { get; set; }
    public ushort Address { get; set; }
    public List<ConnectionInfo> Connections { get; set; }
    public Point Position { get; set; }

    public NeuronInfo(string id, ushort address, Point position)
    {
        Id = id;
        Address = address;
        Position = position;
        Connections = new List<ConnectionInfo>();
    }

    public void AddConnection(ConnectionInfo connection)
    {
        Connections.Add(connection);
    }
}

public class ConnectionInfo
{
    public string Id { get; set; }
    public string SourceNeuronPid { get; set; }
    public NeuronInfo SourceNeuron { get; set; }
    public NeuronInfo TargetNeuron { get; set; }
    public double Strength { get; set; }
    public Timer Timer { get; set; }
    public int Timeout { get; set; }
    public Action<string> ResetConnectionColor { get; set; }

    public ConnectionInfo(string sourceNeuronPid, NeuronInfo targetNeuron, double strength, int timeout, Action<string> resetConnectionColor)
    {
        SourceNeuronPid = sourceNeuronPid;
        TargetNeuron = targetNeuron;
        Strength = strength;
        Timeout = timeout;
        ResetConnectionColor = resetConnectionColor;
        Timer = new Timer(Timeout);
        Timer.Elapsed += Timer_Tick;
        Timer.AutoReset = false;
    }

    public void SetSourceNeuron(NeuronInfo sourceNeuron)
    {
        SourceNeuron = sourceNeuron;
        if (TargetNeuron != null)
            Id = $"{sourceNeuron.Id}-{TargetNeuron.Id}";
    }

    public void SetTargetNeuron(NeuronInfo targetNeuron)
    {
        TargetNeuron = targetNeuron;
        if (SourceNeuron != null)
            Id = $"{SourceNeuron.Id}-{targetNeuron.Id}";
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        Timer.Stop();
        ResetConnectionColor(Id);
    }
}