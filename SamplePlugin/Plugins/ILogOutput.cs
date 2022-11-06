using System;
using System.Threading;

namespace SamplePlugin.Plugins;

public interface ILogOutput
{
    Thread ScanThread { get; }

    void WriteLine(LogMessageType messageType, DateTime ServerDate, string line);
}
