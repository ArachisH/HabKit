using HabKit.Utilities;

using System;
using System.Reflection;

namespace HabKit
{
    public class Program
    {
        public KitOptions Options { get; }

        public Program(string[] args)
        {
            Options = new KitOptions(args);
        }
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

            "             ██╗  ██╗ █████╗ ██████╗ ██╗  ██╗██╗████████╗".AppendLine(ConsoleColor.DarkCyan);
            "             ██║  ██║██╔══██╗██╔══██╗██║ ██╔╝██║╚══██╔══╝".AppendLine(ConsoleColor.DarkCyan);
            "             ███████║███████║██████╔╝█████╔╝ ██║   ██║".AppendLine(ConsoleColor.DarkCyan);
            "             ██╔══██║██╔══██║██╔══██╗██╔═██╗ ██║   ██║".AppendLine(ConsoleColor.DarkCyan);

            "             ██║  ██║██║  ██║██████╔╝██║  ██╗██║   ██║  [".Append(ConsoleColor.DarkCyan);
            ("v" + Assembly.GetExecutingAssembly().GetName().Version).Append();
            "]".AppendLine(ConsoleColor.DarkCyan);

            "             ╚═╝  ╚═╝╚═╝  ╚═╝╚═════╝ ╚═╝  ╚═╝╚═╝   ╚═╝  [".Append(ConsoleColor.DarkCyan);
            "https://www.GitHub.com/ArachisH/HabKit".Append();
            "]".AppendLine(ConsoleColor.DarkCyan);

            Logger.AppendLine();
            Logger.AppendLine();
        }
    }
}