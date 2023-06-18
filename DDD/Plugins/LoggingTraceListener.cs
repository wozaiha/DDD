using System;
using System.Diagnostics;

namespace DDD.Plugins;

public class LoggingTraceListener : TraceListener
{
    public readonly ILogOutput _logOutput;

    public LoggingTraceListener(ILogOutput logOutput)
    {
        _logOutput = logOutput;
    }

    public override void Write(string message)
    {
    }

    public override void WriteLine(string message)
    {
    }

    public override void WriteLine(string message, string category)
    {
        if (category != null && category.Equals("ffxiv_act_plugin", StringComparison.OrdinalIgnoreCase) || category != null && category.Equals("debug-machina", StringComparison.OrdinalIgnoreCase))
        {
            _logOutput.WriteLine(LogMessageType.Debug, DateTime.MinValue, message.Replace(Environment.NewLine, " "));
        }
    }
}
