﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NothingButNeurons.Shared.Consts;

public static class DefaultPorts
{
    public const int SETTINGS_MONITOR = 9000;

    // These should ONLY be used by nodes when launched directly from VS (not passed a port command-line)
    // TODO: validate this by finding references (once the SettingsMonitor/Orchestrator rework is complete)
    public const int IO = 8000;
    public const int DEBUG_SERVER = 8001;
    public const int DEBUG_FILE_WRITER = 8002;
    public const int DEBUG_LOG_VIEWER = 8003;
    public const int VISUALIZER = 8004;
    public const int DESIGNER = 8005;
    public const int ORCHESTRATOR_MONITOR = 9001;
}
