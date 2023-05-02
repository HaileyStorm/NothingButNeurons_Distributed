using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NothingButNeurons.Shared.Serialization;

public class Brain
{
    public static void WriteDataToFile(string filename, byte[] neuronData, byte[] synapseData)
    {
        EnforceNbnExtension(filename);

        using BinaryWriter writer = new(File.Open(filename, FileMode.Create));

        int neuronDataLength = neuronData.Length;
        writer.Write(neuronDataLength);

        writer.Write(neuronData);
        writer.Write(synapseData);
    }

    public static (byte[] neuronData, byte[] synapseData) ReadDataFromFile(string filename)
    {
        EnforceNbnExtension(filename);

        byte[] neuronData, synapseData;

        using BinaryReader reader = new(File.Open(filename, FileMode.Open));

        int neuronDataLength = reader.ReadInt32();

        neuronData = reader.ReadBytes(neuronDataLength);
        synapseData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        
        return (neuronData, synapseData);
    }

    private static void EnforceNbnExtension(string filename)
    {
        string extension = Path.GetExtension(filename);
        if (!extension.Equals(".nbn", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The filename must have a .nbn extension.", nameof(filename));
        }
    }
}
