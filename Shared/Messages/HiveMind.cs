using Proto;

namespace NothingButNeurons.Shared.Messages;

public record SpawnBrainMessage(byte[] NeuronData, byte[] SynapseData) : Message;
public record SpawnBrainAckMessage : Message;
public record ActivateHiveMindMessage : Message;