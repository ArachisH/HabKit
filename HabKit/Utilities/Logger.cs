using System;

namespace HabKit.Utilities
{
    public static class Logger
    {
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
        public static void Append(this object[] values, params ConsoleColor?[] colors)
        {
            for (int i = 0; i < values.Length; i++)
            {
                Append(values[i], colors[i] ?? Console.ForegroundColor);
            }
        }

        public static void AppendLine()
        {
            AppendLine(null);
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

        public static void Write(this object value)
        {
            Write(value, Console.ForegroundColor);
        }
        public static void Write(this object value, ConsoleColor color)
        {
            Append(value, color);
        }

        public static void WriteLine()
        {
            WriteLine(null);
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

        public static void ClearLine(int topOffset = 0)
        {
            Console.SetCursorPosition(0, Console.CursorTop + topOffset);
            int current = Console.CursorTop;

            Console.Write(new string(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, current);
        }
    }
}