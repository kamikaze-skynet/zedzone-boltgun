using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;

namespace MassReactive
{
    // Every line goes to BepInEx\LogOutput.log and to BepInEx\MassReactive.log
    // (rewritten each launch, last launch kept as MassReactive.prev.log).
    // The file is flushed on every line: if the game ever hard-crashes, the
    // last line in MassReactive.log names the step that was running.
    internal static class PLog
    {
        static ManualLogSource _src;
        static StreamWriter _file;
        static readonly object _lock = new object();
        internal static bool Verbose;

        internal static void Open(ManualLogSource src, string version)
        {
            _src = src;
            try
            {
                string path = Path.Combine(Paths.BepInExRootPath, "MassReactive.log");
                string prev = Path.Combine(Paths.BepInExRootPath, "MassReactive.prev.log");
                try { if (File.Exists(path)) File.Copy(path, prev, true); } catch { }
                _file = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
                _file.WriteLine($"MassReactive {version} log - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception e) { _src?.LogWarning("Couldn't open MassReactive.log: " + e.Message); }
        }

        static void Write(LogLevel level, string msg)
        {
            try { _src?.Log(level, msg); } catch { }
            try { lock (_lock) _file?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {level,-7} {msg}"); } catch { }
        }

        internal static void Info(string msg) => Write(LogLevel.Info, msg);
        internal static void Debug(string msg) { if (Verbose) Write(LogLevel.Info, msg); }
        internal static void Warn(string msg) => Write(LogLevel.Warning, msg);
        internal static void Error(string msg, Exception e = null) => Write(LogLevel.Error, e == null ? msg : msg + ": " + e);
    }
}
