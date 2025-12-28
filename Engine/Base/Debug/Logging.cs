using System;
using System.Collections.Generic;

namespace MagicThing.Engine.Base.Debug;

#nullable enable

public class LogManager()
{
    public LogLevel LogMode = LogLevel.Debug;
    public List<LogEntry> Logs = new List<LogEntry>();

    public void Log(string text, LogLevel level = LogLevel.Debug)
    {
        Logs.Add(new LogEntry
        {
            Message = text,
            Originating = null,
            Level = level,
            Time = DateTime.Now,
        });

        if ((int)level <= (int)LogMode)
        {
            Console.WriteLine(text);
        }
    }

    public void Log(string text, string originatingSystem, LogLevel level)
    {
        Logs.Add(new LogEntry
        {
            Message = text,
            Originating = originatingSystem,
            Level = level,
            Time = DateTime.Now,
        });

        if ((int)level <= (int)LogMode)
        {
            Console.WriteLine($"[{originatingSystem}] {text}");
        }
    }
    
    /// <summary>
    /// Shorthand method for Log with debug level
    /// </summary>
    public void Debug(string text) {Log(text, LogLevel.Debug);}
    public void Debug(string text, string originatingSystem) {Log(text, originatingSystem, LogLevel.Debug);}
    /// <summary>
    /// Shorthand method for Log with release level
    /// </summary>
    public void Release(string text) {Log(text, LogLevel.Release);}
    public void Release(string text, string originatingSystem) {Log(text, originatingSystem, LogLevel.Release);}
    /// <summary>
    /// Shorthand method for Log with verbose level
    /// </summary>
    public void Verbose(string text) {Log(text, LogLevel.Verbose);}
    public void Verbose(string text, string originatingSystem) {Log(text, originatingSystem, LogLevel.Verbose);}

    public void AssertFailure(string text, string originatingSystem , LogLevel level = LogLevel.Debug)
    {
        Log(text, originatingSystem, level);
        throw new Exception($"Assert failure: {text}");
    }
}

public enum LogLevel
{
    None,
    Release,
    Debug,
    Verbose,       
    VerboseExtra
}

public struct LogEntry
{
    public string Message;
    public string? Originating;
    public LogLevel Level;
    public DateTime Time;
}