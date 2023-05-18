using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace NothingButNeurons.CCSL;

public static class Console
{
    public static void CombinedWriteLine(string line)
    {
        System.Diagnostics.Debug.WriteLine(line);
        System.Console.WriteLine(line);
    }
}
