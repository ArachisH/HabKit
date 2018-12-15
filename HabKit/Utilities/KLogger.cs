using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace HabKit.Utilities
{
    public static class KLogger
    {
        public static void Error(object value)
        {
            Write("[Error] ", ConsoleColor.Red);
            if (value is Exception exception)
            {
                AppendLine($"{exception.GetType().Name}: {exception.Message}\r\n{exception.StackTrace}");
            }
            else AppendLine(value);
        }
        public static void System(object value)
        {
            Write("[System] ", ConsoleColor.Yellow);
            AppendLine(value);
        }
        public static void Database(object value)
        {
            Write("[Database] ", ConsoleColor.Cyan);
            AppendLine(value);
        }

        public static void Append(this object value)
        {
            Append(value, Console.ForegroundColor);
        }
        public static void Append(this object value, ConsoleColor color)
        {
            ConsoleColor currentColor = Console.ForegroundColor;
            Console.ForegroundColor = color;

            Console.Write(value);
            Console.ForegroundColor = currentColor;
        }
        public static void Append(this ITuple values, params ConsoleColor?[] colors)
        {
            if (colors.Length == 0) colors = null;
            for (int i = 0; i < values.Length; i++)
            {
                Append(values[i], colors?[i] ?? Console.ForegroundColor);
            }
        }

        public static void AppendLine(this object value)
        {
            AppendLine(value, Console.ForegroundColor);
        }
        public static void AppendLine(this object value, ConsoleColor color)
        {
            if (value != null)
            {
                Append(value, color);
            }
            Console.WriteLine();
        }
        public static void AppendLine(this ITuple values, params ConsoleColor?[] colors)
        {
            Append(values, colors);
            Console.WriteLine();
        }

        public static void Write(this object value)
        {
            Write(value, Console.ForegroundColor);
        }
        public static void Write(this object value, ConsoleColor color)
        {
            AppendTimestamp();
            Append(value, color);
        }
        public static void Write(this ITuple values, params ConsoleColor?[] colors)
        {
            AppendTimestamp();
            Append(values, colors);
        }

        public static void WriteLine(this object value)
        {
            WriteLine(value, Console.ForegroundColor);
        }
        public static void WriteLine(this object value, ConsoleColor color)
        {
            if (value != null)
            {
                Write(value, color);
            }
            Console.WriteLine();
        }
        public static void WriteLine(this ITuple values, params ConsoleColor?[] colors)
        {
            Write(values, colors);
            Console.WriteLine();
        }

        public static void EmptyLine()
        {
            Console.WriteLine();
        }
        public static void SetCursorVisibility(bool isCursorVisible)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string arguments = isCursorVisible ? "cnorm -- normal" : "civis -- invisible";
                Process.Start("tput", arguments);
            }
            else Console.CursorVisible = isCursorVisible;
        }

        public static IDisposable Lock()
        {
            return new LogLocker();
        }
        public static void AppendTimestamp()
        {
            Console.Write($"[{DateTime.UtcNow:MM/dd/yyyy HH:mm:ss} UTC] ");
        }
        public static void ClearLine(int topOffset = 0, int leftOffset = 0)
        {
            Console.SetCursorPosition(leftOffset, Console.CursorTop + topOffset);
            int current = Console.CursorTop;

            Console.Write(new string(' ', Console.BufferWidth - leftOffset));
            Console.SetCursorPosition(leftOffset, current);
        }

        private class LogLocker : IDisposable
        {
            private static readonly SemaphoreSlim _semaphore;

            static LogLocker()
            {
                _semaphore = new SemaphoreSlim(1, 1);
            }
            public LogLocker()
            {
                _semaphore.Wait();
            }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
}