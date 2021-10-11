using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace D2RSO
{
    internal static class Logger
    {
        private static string _filePath;

        internal static void Initialize(string filePath)
        {
            _filePath = filePath;
        }

        internal static void Log(string message, [CallerMemberName] string fname = null)
        {
            File.WriteAllText(_filePath, $"[{DateTime.Now:T}]:[{fname}] {message}");
        }

        internal static void Log(Exception ex, [CallerMemberName]string fname = null)
        {
            File.WriteAllText(_filePath, $"[{DateTime.Now:T}]:[{fname}] {ex.ToString()}");
        }

    }
}
