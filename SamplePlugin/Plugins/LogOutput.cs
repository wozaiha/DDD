#define TRACE
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace DDD.Plugins;

public class LogOutput : ILogOutput
{
    public readonly Queue<string> _LogQueue = new Queue<string>(1000);

    public readonly object _LogQueueLock = new object();

    public int _logIndex;

    public bool _disableHash;

    public string _logFileName;

    public DateTime _lastLogTime = DateTime.MinValue;

    public readonly object _lastLogTimeLock = new object();

    public static readonly uint[] _lookup32 = u_49151();

    public Thread ScanThread { get; private set; }

    //public LogOutput(ISettingsMediator settingsManager, IActWrapper actWrapper, IBenchmarkRepository benchmarkRepository)
    //{
    //	_settingsManager = settingsManager;
    //	_actWrapper = actWrapper;
    //       ScanThread = new Thread(Run)
    //	{
    //		IsBackground = true,
    //		Name = GetType().FullName
    //	};
    //}

    public string ConfigureLogFile()
    {
        //try
        //{
        //	using SHA256 sHA = SHA256.Create();
        //	sHA.ComputeHash(Encoding.UTF8.GetBytes("1234567890"));
        //}
        //catch (Exception)
        //{
        //	Trace.WriteLine("ERROR: Windows feature is missing, disabling support for FFLogs Uploads.", "FFXIV_ACT_Plugin");
        //	_disableHash = true;
        //}
        //      Version version = typeof(ILogOutput).Assembly.GetName().Version;
        //_logFileName = Path.Combine(_logFileFolder, $"Network_{version.Major}{version.Minor}{version.Build}0{version.Revision}_{DateTime.Today:yyyyMMdd}.log");
        //if (!Directory.Exists(_logFileFolder))
        //{
        //	Directory.CreateDirectory(_settingsManager.DataCollectionSettings.LogFileFolder);
        //}
        //bool flag = File.Exists(_logFileName);
        //if (!flag)
        //{
        //	File.AppendAllText(_logFileName, "");
        //}
        //if (_logFilePath != _logFileName || !flag)
        //{
        //	_logFilePath = _logFileName;
        //      }
        return _logFileName;
    }

    public void Run(object parameter)
    {
    }//Discarded unreachable code: IL_0001, IL_0009, IL_0019, IL_001a, IL_0027, IL_0034, IL_004c, IL_01d1, IL_01e4


    public void WriteLine(LogMessageType messageType, DateTime ServerDate, string line)
    {
        lock (_lastLogTimeLock)
        {
            if (ServerDate == DateTime.MinValue)
            {
                if (_lastLogTime == DateTime.MinValue)
                {
                    _lastLogTime = DateTime.Now;
                }
                ServerDate = _lastLogTime;
            }
            else
            {
                _lastLogTime = ServerDate;
            }
        }
        var array = new string[5];
        var num = (int)messageType;
        array[0] = num.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0');
        array[1] = "|";
        array[2] = ServerDate.ToString("O");
        array[3] = "|";
        array[4] = line.Replace('\0', ' ');
        var text = string.Concat(array);
        lock (_LogQueueLock)
        {
            if (messageType == LogMessageType.Version || messageType == LogMessageType.Territory)
            {
                Interlocked.Exchange(ref _logIndex, 0);
            }
            text = text + "|" + u_65535(text + "|" + Interlocked.Increment(ref _logIndex).ToString(CultureInfo.InvariantCulture));
            _LogQueue.Enqueue(text);
        }
    }

    public string u_65535(string text)
    {
        if (_disableHash)
        {
            return "0";
        }
        using var sHA = SHA256.Create();
        return u_49152(sHA.ComputeHash(Encoding.UTF8.GetBytes(text)));
    }

    public static uint[] u_49151()
    {
        var array = new uint[256];
        for (var i = 0; i < 256; i++)
        {
            var text = $"{i:x2}";
            array[i] = text[0] + ((uint)text[1] << 16);
        }
        return array;
    }

    public static string u_49152(byte[] bytes)
    {
        var lookup = _lookup32;
        var array = new char[16];
        for (var i = 0; i < array.Length / 2; i++)
        {
            var num = lookup[bytes[i]];
            array[2 * i] = (char)num;
            array[2 * i + 1] = (char)(num >> 16);
        }
        return new string(array);
    }
}
