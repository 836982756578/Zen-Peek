// Zen Peek - Hızlı Dosya Önizleme (macOS QuickLook)
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.IO;

namespace ZenPeek
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string _logPath;

        static Logger()
        {
            try
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                _logPath = Path.Combine(dir, "zenpeek.log");
            }
            catch
            {
                _logPath = Path.Combine(Path.GetTempPath(), "zenpeek.log");
            }
        }

        public static void Log(string message)
        {
            try
            {
                lock (_lock)
                {
                    string line = string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] {1}\r\n", DateTime.Now, message);
                    File.AppendAllText(_logPath, line);
                }
            }
            catch { }
        }
    }
}
