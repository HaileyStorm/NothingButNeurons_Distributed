using Google.Protobuf.Reflection;
using NothingButNeurons.Shared.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NothingButNeurons.Shared.Consts;

public static class Meta
{
    public static FileDescriptor[] FileDescriptors = new FileDescriptor[4] { DebuggerReflection.Descriptor, NeuronsReflection.Descriptor, IOReflection.Descriptor, SettingsReflection.Descriptor };
}
