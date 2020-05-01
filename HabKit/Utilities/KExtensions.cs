using System;

namespace HabKit.Utilities
{
    public static class KExtensions
    {
        public static bool WriteResult(this bool value)
        {
            KLogger.AppendLine(value ? "Success!" : "Failed!", value ? ConsoleColor.Green : ConsoleColor.Red);
            return value;
        }
    }
}