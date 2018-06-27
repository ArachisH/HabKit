using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace HabKit.Utilities
{
    public static class KitLogger
    {
        public static void Write(this object value)
        {
            Write(value, Console.ForegroundColor);
        }
        public static void Write(this object value, ConsoleColor color)
        {
            ConsoleColor currentColor = Console.ForegroundColor;
            Console.ForegroundColor = color;

            Console.Write(value);
            Console.ForegroundColor = currentColor;
        }
        public static void Write(this ITuple values, params ConsoleColor?[] colors)
        {
            for (int i = 0; i < values.Length; i++)
            {
                Write(values[i], (colors[i] ?? Console.ForegroundColor));
            }
        }

        public static void WriteLine()
        {
            Console.WriteLine();
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
            WriteLine();
        }

        public static void SetCursorVisibility(bool isVisible)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string arguments = (isVisible ? "cnorm -- normal" : "civis -- invisible");
                Process.Start("tput", arguments);
            }
            else Console.CursorVisible = isVisible;
        }
        public static void ClearLine(int topOffset = 0, int leftOffset = 0)
        {
            Console.SetCursorPosition(leftOffset, Console.CursorTop + topOffset);
            int current = Console.CursorTop;

            Console.Write(new string(' ', Console.BufferWidth - leftOffset));
            Console.SetCursorPosition(leftOffset, current);
        }
    }
}