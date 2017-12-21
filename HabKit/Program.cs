using HabKit.Utilities;

using System;
using System.Reflection;

namespace HabKit
{
    public class Program
    {
        public Program(string[] args)
        { }
        public static void Main(string[] args)
        {
            try
            {
                Console.CursorVisible = false;
                new Program(args).Run();
            }
            finally { Console.CursorVisible = true; }
        }

        private void Run()
        {
            WriteLogo();
            Console.ReadLine();
        }
        private void WriteLogo()
        {
            Logger.AppendLine();
            Logger.AppendLine();

            Logger.AppendLine("             ██╗  ██╗ █████╗ ██████╗ ██╗  ██╗██╗████████╗", ConsoleColor.DarkCyan);
            Logger.AppendLine("             ██║  ██║██╔══██╗██╔══██╗██║ ██╔╝██║╚══██╔══╝", ConsoleColor.DarkCyan);
            Logger.AppendLine("             ███████║███████║██████╔╝█████╔╝ ██║   ██║", ConsoleColor.DarkCyan);
            Logger.AppendLine("             ██╔══██║██╔══██║██╔══██╗██╔═██╗ ██║   ██║", ConsoleColor.DarkCyan);

            Logger.Append("             ██║  ██║██║  ██║██████╔╝██║  ██╗██║   ██║  [", ConsoleColor.DarkCyan);
            Logger.Append("v" + Assembly.GetExecutingAssembly().GetName().Version);
            Logger.AppendLine("]", ConsoleColor.DarkCyan);

            Logger.Append("             ╚═╝  ╚═╝╚═╝  ╚═╝╚═════╝ ╚═╝  ╚═╝╚═╝   ╚═╝  [", ConsoleColor.DarkCyan);
            Logger.Append("https://www.GitHub.com/ArachisH/HabKit");
            Logger.AppendLine("]", ConsoleColor.DarkCyan);

            Logger.AppendLine();
            Logger.AppendLine();
        }
    }
}