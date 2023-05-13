using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NothingButNeurons.Shared.Consts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NothingButNeurons.Shared.Messages;

public static class Helpers
{
    /// <summary>
    /// Unpacks a Protobuf Any into its original message type. Useful when the type isn't known (compile time).
    /// </summary>
    /// <param name="packed">The packed Protobuf message</param>
    /// <returns>The unpacked (converted) message.</returns>
    public static IMessage UnpackAny(Any packed)
    {
        return packed.Unpack(Google.Protobuf.Reflection.TypeRegistry.FromFiles(Meta.FileDescriptors));
    }
}